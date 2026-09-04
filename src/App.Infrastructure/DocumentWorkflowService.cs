using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using App.Application;
using App.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure;

public sealed class PostgresGenerationQueue : IGenerationQueue { public Task EnqueueAsync(Guid generationJobId, CancellationToken cancellationToken) => Task.CompletedTask; }
public sealed class PostgresCompilationQueue : ICompilationQueue { public Task EnqueueAsync(Guid compileJobId, CancellationToken cancellationToken) => Task.CompletedTask; }

public sealed class DocumentWorkflowService(AppDbContext db, IGenerationQueue generationQueue, ICompilationQueue compilationQueue) : IDocumentWorkflowService
{
    public async Task<IReadOnlyList<TemplateView>> GetTemplatesAsync(string workspaceKey, CancellationToken ct)
    {
        var templates = await db.DocumentTemplates.AsNoTracking().Where(x => x.WorkspaceKey == workspaceKey && !x.IsArchived).OrderBy(x => x.Name).ToListAsync(ct);
        var versions = await db.DocumentTemplateVersions.AsNoTracking().Where(x => x.WorkspaceKey == workspaceKey).ToListAsync(ct);
        return templates.Select(x => { var v = versions.Single(y => y.TemplateId == x.Id && y.Version == x.CurrentVersion); return new TemplateView(x.Id, x.Name, v.Version, v.Id, v.Tex); }).ToArray();
    }

    public async Task<RuleView> GetRulesAsync(string workspaceKey, CancellationToken ct)
    {
        var document = await db.UserRuleDocuments.AsNoTracking().SingleAsync(x => x.WorkspaceKey == workspaceKey, ct);
        var version = await db.UserRuleVersions.AsNoTracking().SingleAsync(x => x.WorkspaceKey == workspaceKey && x.RuleDocumentId == document.Id && x.Version == document.CurrentVersion, ct);
        return new(document.Id, version.Version, version.Id, version.Markdown);
    }

    public async Task<JobView?> CreateGenerationAsync(string workspaceKey, Guid sourceItemId, Guid templateVersionId, Guid ruleVersionId, string? instruction, CancellationToken ct)
    {
        if (instruction?.Length > 5_000 || await db.GenerationJobs.CountAsync(x => x.WorkspaceKey == workspaceKey, ct) >= 10) return null;
        var item = await db.SourceItems.AsNoTracking().SingleOrDefaultAsync(x => x.WorkspaceKey == workspaceKey && x.Id == sourceItemId, ct);
        var templateExists = await db.DocumentTemplateVersions.AnyAsync(x => x.WorkspaceKey == workspaceKey && x.Id == templateVersionId, ct);
        var rulesExist = await db.UserRuleVersions.AnyAsync(x => x.WorkspaceKey == workspaceKey && x.Id == ruleVersionId, ct);
        if (item is null || !templateExists || !rulesExist) return null;
        var snapshot = JsonSerializer.Serialize(new { item.DisplayTitle, parsedData = JsonSerializer.Deserialize<object>(item.ParsedDataJson) }, JsonDefaults.Web);
        var job = new GenerationJob { WorkspaceKey = workspaceKey, SourceItemId = sourceItemId, TemplateVersionId = templateVersionId, RuleVersionId = ruleVersionId, SourceSnapshotJson = snapshot, SystemRulesHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("generation-v1"))).ToLowerInvariant(), Model = "demo-deterministic-v1", UserInstruction = instruction };
        db.GenerationJobs.Add(job); await db.SaveChangesAsync(ct); await generationQueue.EnqueueAsync(job.Id, ct); return Map(job);
    }

    public async Task<JobView?> GetGenerationAsync(string workspaceKey, Guid id, CancellationToken ct) => Map(await db.GenerationJobs.AsNoTracking().SingleOrDefaultAsync(x => x.WorkspaceKey == workspaceKey && x.Id == id, ct));
    public async Task<DocumentView?> GetDocumentAsync(string workspaceKey, Guid id, CancellationToken ct)
    {
        var document = await db.GeneratedDocuments.AsNoTracking().SingleOrDefaultAsync(x => x.WorkspaceKey == workspaceKey && x.Id == id, ct); if (document is null) return null;
        var version = await db.GeneratedDocumentVersions.AsNoTracking().SingleAsync(x => x.WorkspaceKey == workspaceKey && x.DocumentId == id && x.Version == document.CurrentVersion, ct);
        return new(document.Id, version.Version, version.Id, version.Tex, version.Origin);
    }
    public async Task<JobView?> CreateCompileAsync(string workspaceKey, Guid documentVersionId, CancellationToken ct)
    {
        if (await db.CompileJobs.CountAsync(x => x.WorkspaceKey == workspaceKey, ct) >= 10 || !await db.GeneratedDocumentVersions.AnyAsync(x => x.WorkspaceKey == workspaceKey && x.Id == documentVersionId, ct)) return null;
        var job = new CompileJob { WorkspaceKey = workspaceKey, DocumentVersionId = documentVersionId }; db.CompileJobs.Add(job); await db.SaveChangesAsync(ct); await compilationQueue.EnqueueAsync(job.Id, ct); return Map(job);
    }
    public async Task<JobView?> GetCompileAsync(string workspaceKey, Guid id, CancellationToken ct) => Map(await db.CompileJobs.AsNoTracking().SingleOrDefaultAsync(x => x.WorkspaceKey == workspaceKey && x.Id == id, ct));
    private static JobView? Map(GenerationJob? x) => x is null ? null : new(x.Id, x.Status, x.Error, x.GeneratedDocumentId);
    private static JobView? Map(CompileJob? x) => x is null ? null : new(x.Id, x.Status, x.Error, x.PdfArtifactId);
}
