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

    public async Task<bool> TryAddAsync(
        CvCompileJob job,
        int maximumJobsPerWorkspace,
        CancellationToken cancellationToken)
    {
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

    public Task<string> GetTexAsync(
        CvCompileJob job,
        CancellationToken cancellationToken) =>
        db.GeneratedCvVersions
            .AsNoTracking()
            .Where(version =>
                version.Id == job.GeneratedCvVersionId &&
                version.WorkspaceKey == job.WorkspaceKey)
            .Select(version => version.Tex)
            .SingleAsync(cancellationToken);

    public async Task CompleteAsync(
        CvCompileJob job,
        StoredArtifact storedArtifact,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken)
    {
        var artifact = new PdfArtifact
        {
            Id = storedArtifact.Id,
            WorkspaceKey = job.WorkspaceKey,
            GeneratedCvVersionId = job.GeneratedCvVersionId,
            ObjectKey = storedArtifact.Key,
            Sha256 = storedArtifact.Sha256,
            Size = storedArtifact.Size
        };
        db.PdfArtifacts.Add(artifact);
        job.PdfArtifactId = artifact.Id;
        job.MarkSucceeded(completedAt);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task SaveAsync(CvCompileJob job, CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);
}
