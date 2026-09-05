using System.Data;
using App.Application;
using App.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure;

public sealed class CvGenerationStore(AppDbContext db) : ICvGenerationStore
{
    public async Task<CvGenerationRequestSource?> GetRequestSourceAsync(
        string workspaceKey,
        RequestCvGeneration request,
        CancellationToken cancellationToken)
    {
        var source = await db.JobPostings
            .AsNoTracking()
            .Where(posting =>
                posting.WorkspaceKey == workspaceKey && posting.Id == request.JobPostingId)
            .Select(posting => new CvGenerationRequestSource(
                posting.Title,
                posting.NormalizedDataJson))
            .SingleOrDefaultAsync(cancellationToken);
        if (source is null) return null;

        var templateExists = await db.CvTemplateVersions
            .AsNoTracking()
            .AnyAsync(
                version => version.WorkspaceKey == workspaceKey &&
                           version.Id == request.TemplateVersionId,
                cancellationToken);
        if (!templateExists) return null;

        var candidateRulesExist = await db.CandidateRuleVersions
            .AsNoTracking()
            .AnyAsync(
                version => version.WorkspaceKey == workspaceKey &&
                           version.Id == request.CandidateRuleVersionId,
                cancellationToken);
        return candidateRulesExist ? source : null;
    }

    public async Task<bool> TryAddAsync(
        CvGenerationJob job,
        int maximumJobsPerWorkspace,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        var count = await db.CvGenerationJobs.CountAsync(
            candidate => candidate.WorkspaceKey == job.WorkspaceKey,
            cancellationToken);
        if (count >= maximumJobsPerWorkspace) return false;

        db.CvGenerationJobs.Add(job);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public Task<CvGenerationJob?> GetAsync(
        string workspaceKey,
        Guid id,
        CancellationToken cancellationToken) =>
        db.CvGenerationJobs
            .AsNoTracking()
            .SingleOrDefaultAsync(
                job => job.WorkspaceKey == workspaceKey && job.Id == id,
                cancellationToken);

    public async Task<CvGenerationJob?> ClaimNextAsync(
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        var job = await db.CvGenerationJobs
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

    public async Task<CvGenerationProcessingInputs> GetProcessingInputsAsync(
        CvGenerationJob job,
        CancellationToken cancellationToken)
    {
        var template = await db.CvTemplateVersions
            .AsNoTracking()
            .SingleAsync(
                version => version.Id == job.CvTemplateVersionId &&
                           version.WorkspaceKey == job.WorkspaceKey,
                cancellationToken);
        var rules = await db.CandidateRuleVersions
            .AsNoTracking()
            .SingleAsync(
                version => version.Id == job.CandidateRuleVersionId &&
                           version.WorkspaceKey == job.WorkspaceKey,
                cancellationToken);
        return new CvGenerationProcessingInputs(template.Tex, rules.Markdown);
    }

    public async Task CompleteAsync(
        CvGenerationJob job,
        AiCvGenerationResult result,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken)
    {
        var generatedCv = new GeneratedCv
        {
            WorkspaceKey = job.WorkspaceKey,
            JobPostingId = job.JobPostingId,
            CurrentVersion = 1
        };
        db.GeneratedCvs.Add(generatedCv);
        db.GeneratedCvVersions.Add(new GeneratedCvVersion
        {
            WorkspaceKey = job.WorkspaceKey,
            GeneratedCvId = generatedCv.Id,
            Version = 1,
            Tex = result.Tex,
            Origin = result.Origin
        });
        job.GeneratedCvId = generatedCv.Id;
        job.MarkSucceeded(completedAt);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task SaveAsync(CvGenerationJob job, CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);
}
