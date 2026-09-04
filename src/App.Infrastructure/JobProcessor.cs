using System.Data;
using App.Application;
using App.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure;

public sealed class JobProcessor(AppDbContext db, IAiDocumentGenerator generator, ITexCompiler compiler, IArtifactStorage storage)
{
    public async Task<bool> ProcessNextGenerationAsync(CancellationToken ct)
    {
        var job = await ClaimGenerationAsync(ct); if (job is null) return false;
        try
        {
            var template = await db.DocumentTemplateVersions.AsNoTracking().SingleAsync(x => x.Id == job.TemplateVersionId && x.WorkspaceKey == job.WorkspaceKey, ct);
            var rules = await db.UserRuleVersions.AsNoTracking().SingleAsync(x => x.Id == job.RuleVersionId && x.WorkspaceKey == job.WorkspaceKey, ct);
            var tex = await generator.GenerateAsync(job.SourceSnapshotJson, template.Tex, rules.Markdown, job.UserInstruction, ct); TexSafety.Validate(tex);
            var document = new GeneratedDocument { WorkspaceKey = job.WorkspaceKey, SourceItemId = job.SourceItemId, CurrentVersion = 1 };
            db.GeneratedDocuments.Add(document); db.GeneratedDocumentVersions.Add(new GeneratedDocumentVersion { WorkspaceKey = job.WorkspaceKey, DocumentId = document.Id, Version = 1, Tex = tex, Origin = "DEMO_GENERATOR" });
            job.GeneratedDocumentId = document.Id; Complete(job); await db.SaveChangesAsync(ct); return true;
        }
        catch (Exception ex) when (ex is InvalidOperationException or FormatException) { Fail(job, ex.Message); await db.SaveChangesAsync(ct); return true; }
    }

    public async Task<bool> ProcessNextCompileAsync(CancellationToken ct)
    {
        var job = await ClaimCompileAsync(ct); if (job is null) return false;
        try
        {
            var version = await db.GeneratedDocumentVersions.AsNoTracking().SingleAsync(x => x.Id == job.DocumentVersionId && x.WorkspaceKey == job.WorkspaceKey, ct);
            var pdf = await compiler.CompileAsync(version.Tex, ct); var artifact = new PdfArtifact { WorkspaceKey = job.WorkspaceKey, DocumentVersionId = version.Id, ObjectKey = "pending", Sha256 = "pending" };
            var saved = await storage.SaveAsync(job.WorkspaceKey, artifact.Id, pdf, ct); artifact.ObjectKey = saved.Key; artifact.Sha256 = saved.Sha256; artifact.Size = saved.Size;
            db.PdfArtifacts.Add(artifact); job.PdfArtifactId = artifact.Id; Complete(job); await db.SaveChangesAsync(ct); return true;
        }
        catch (Exception ex) when (ex is InvalidOperationException or FormatException) { Fail(job, ex.Message); await db.SaveChangesAsync(ct); return true; }
    }

    private async Task<GenerationJob?> ClaimGenerationAsync(CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct); var now = DateTimeOffset.UtcNow;
        var job = await db.GenerationJobs.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync(x => x.Status == JobStatus.Queued || (x.Status == JobStatus.Running && x.LeaseUntil < now), ct);
        if (job is null) return null; Claim(job, now.AddMinutes(2)); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return job;
    }
    private async Task<CompileJob?> ClaimCompileAsync(CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct); var now = DateTimeOffset.UtcNow;
        var job = await db.CompileJobs.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync(x => x.Status == JobStatus.Queued || (x.Status == JobStatus.Running && x.LeaseUntil < now), ct);
        if (job is null) return null; Claim(job, now.AddMinutes(1)); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return job;
    }
    private static void Claim(LeasedJob job, DateTimeOffset until) { job.Status = JobStatus.Running; job.AttemptCount++; job.LeaseUntil = until; }
    private static void Complete(LeasedJob job) { job.Status = JobStatus.Succeeded; job.CompletedAt = DateTimeOffset.UtcNow; job.LeaseUntil = null; }
    private static void Fail(LeasedJob job, string message) { job.Status = JobStatus.Failed; job.Error = message[..Math.Min(500, message.Length)]; job.CompletedAt = DateTimeOffset.UtcNow; job.LeaseUntil = null; }
}
