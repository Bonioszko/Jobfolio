using System.Data;
using App.Application;
using App.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure;

public sealed class CvCompilationStore(AppDbContext db) : ICvCompilationStore
{
    public Task<bool> VersionExistsAsync(
        string workspaceKey,
        Guid generatedCvVersionId,
        CancellationToken cancellationToken) =>
        db.GeneratedCvVersions
            .AsNoTracking()
            .AnyAsync(
                version => version.WorkspaceKey == workspaceKey &&
                           version.Id == generatedCvVersionId,
                cancellationToken);

    public Task<bool> TemplateVersionExistsAsync(
        string workspaceKey,
        Guid templateVersionId,
        CancellationToken cancellationToken) =>
        db.CvTemplateVersions
            .AsNoTracking()
            .AnyAsync(
                version => version.WorkspaceKey == workspaceKey &&
                           version.Id == templateVersionId,
                cancellationToken);

    public async Task<bool> TryAddAsync(
        CvCompileJob job,
        int maximumJobsPerWorkspace,
        CancellationToken cancellationToken)
    {
        if (!job.HasExactlyOneSource)
        {
            throw new ArgumentException(
                "A compile job must reference exactly one source version.",
                nameof(job));
        }

        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        var count = await db.CvCompileJobs.CountAsync(
            candidate => candidate.WorkspaceKey == job.WorkspaceKey &&
                         (candidate.Status == JobStatus.Queued ||
                          candidate.Status == JobStatus.Running),
            cancellationToken);
        if (count >= maximumJobsPerWorkspace) return false;

        db.CvCompileJobs.Add(job);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public Task<CvCompileJob?> GetAsync(
        string workspaceKey,
        Guid id,
        CancellationToken cancellationToken) =>
        db.CvCompileJobs
            .AsNoTracking()
            .SingleOrDefaultAsync(
                job => job.WorkspaceKey == workspaceKey && job.Id == id,
                cancellationToken);

    public async Task<CvCompileJob?> ClaimNextAsync(
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        var job = await db.CvCompileJobs
            .OrderBy(candidate => candidate.CreatedAt)
            .ThenBy(candidate => candidate.Id)
            .FirstOrDefaultAsync(
                candidate => candidate.Status == JobStatus.Queued ||
                             candidate.Status == JobStatus.Running && candidate.LeaseUntil <= now,
                cancellationToken);

        if (job is null) return null;

        job.MarkClaimed(now, leaseDuration);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return job;
    }

    public async Task<string> GetTexAsync(
        CvCompileJob job,
        CancellationToken cancellationToken)
    {
        if (job.GeneratedCvVersionId is Guid generatedVersionId)
        {
            return await db.GeneratedCvVersions
            .AsNoTracking()
            .Where(version =>
                version.Id == generatedVersionId &&
                version.WorkspaceKey == job.WorkspaceKey)
            .Select(version => version.Tex)
            .SingleAsync(cancellationToken);
        }

        if (job.CvTemplateVersionId is Guid templateVersionId)
        {
            return await db.CvTemplateVersions
                .AsNoTracking()
                .Where(version => version.Id == templateVersionId && version.WorkspaceKey == job.WorkspaceKey)
                .Select(version => version.Tex)
                .SingleAsync(cancellationToken);
        }

        throw new InvalidOperationException("A compile job has no source version.");
    }

    public async Task CompleteAsync(
        CvCompileJob job,
        StoredArtifact storedArtifact,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken)
    {
        if (!job.HasExactlyOneSource)
        {
            throw new InvalidOperationException(
                "A compile job must reference exactly one source version.");
        }

        var artifact = new PdfArtifact
        {
            Id = storedArtifact.Id,
            WorkspaceKey = job.WorkspaceKey,
            GeneratedCvVersionId = job.GeneratedCvVersionId,
            CvTemplateVersionId = job.CvTemplateVersionId,
            ObjectKey = storedArtifact.Key,
            Sha256 = storedArtifact.Sha256,
            Size = storedArtifact.Size
        };
        if (!artifact.HasExactlyOneSource)
        {
            throw new InvalidOperationException(
                "A PDF artifact must reference exactly one source version.");
        }
        db.PdfArtifacts.Add(artifact);
        job.PdfArtifactId = artifact.Id;
        job.MarkSucceeded(completedAt);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task SaveAsync(CvCompileJob job, CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);
}
