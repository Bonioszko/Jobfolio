using App.Application;
using App.Domain;
using App.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace App.Tests;

public sealed class CvCompilationServiceTests
{
    [Fact]
    public async Task Template_request_persists_job_before_enqueuing_identifier()
    {
        await using var db = CreateDbContext();
        const string workspace = "demo:template-compilation";
        var version = new CvTemplateVersion
        {
            WorkspaceKey = workspace,
            CvTemplateId = Guid.NewGuid(),
            Version = 1,
            Tex = "\\documentclass{article}"
        };
        db.CvTemplateVersions.Add(version);
        await db.SaveChangesAsync();
        var queue = new InspectingCompilationQueue(db);
        var service = CreateService(db, queue);

        var result = await service.RequestTemplateAsync(
            workspace,
            version.Id,
            CancellationToken.None);

        Assert.Equal(RequestCvCompilationOutcome.Accepted, result.Outcome);
        Assert.Equal(result.Job!.Id, queue.EnqueuedJobId);
        Assert.True(queue.JobExistedWhenEnqueued);
        var job = await db.CvCompileJobs.SingleAsync();
        Assert.Equal(version.Id, job.CvTemplateVersionId);
        Assert.Null(job.GeneratedCvVersionId);
        Assert.True(job.HasExactlyOneSource);
    }

    [Fact]
    public async Task Template_request_rejects_another_workspaces_version()
    {
        await using var db = CreateDbContext();
        var version = new CvTemplateVersion
        {
            WorkspaceKey = "demo:other",
            CvTemplateId = Guid.NewGuid(),
            Version = 1,
            Tex = "\\documentclass{article}"
        };
        db.CvTemplateVersions.Add(version);
        await db.SaveChangesAsync();
        var queue = new InspectingCompilationQueue(db);
        var service = CreateService(db, queue);

        var result = await service.RequestTemplateAsync(
            "demo:current",
            version.Id,
            CancellationToken.None);

        Assert.Equal(RequestCvCompilationOutcome.InvalidInput, result.Outcome);
        Assert.Null(queue.EnqueuedJobId);
        Assert.Empty(db.CvCompileJobs);
    }

    private static CvCompilationService CreateService(
        AppDbContext db,
        ICvCompilationQueue queue) =>
        new(
            new CvCompilationStore(db),
            queue,
            new CvWorkflowSettings(10, 10, 5_000, 20_000, "test"));

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    private sealed class InspectingCompilationQueue(AppDbContext db) : ICvCompilationQueue
    {
        public Guid? EnqueuedJobId { get; private set; }
        public bool JobExistedWhenEnqueued { get; private set; }

        public async Task EnqueueAsync(Guid cvCompileJobId, CancellationToken cancellationToken)
        {
            EnqueuedJobId = cvCompileJobId;
            JobExistedWhenEnqueued = await db.CvCompileJobs
                .AnyAsync(job => job.Id == cvCompileJobId, cancellationToken);
        }
    }
}
