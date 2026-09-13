using App.Application;
using App.Domain;
using App.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace App.Tests;

public sealed class CvCompilationServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 13, 10, 0, 0, TimeSpan.Zero);

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
            UserMode.Demo,
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
            UserMode.Demo,
            version.Id,
            CancellationToken.None);

        Assert.Equal(RequestCvCompilationOutcome.InvalidInput, result.Outcome);
        Assert.Null(queue.EnqueuedJobId);
        Assert.Empty(db.CvCompileJobs);
    }

    [Fact]
    public async Task Demo_request_is_rejected_when_shared_compilation_window_is_full()
    {
        await using var db = CreateDbContext();
        const string workspace = "demo:current";
        var version = CreateGeneratedVersion(workspace);
        db.GeneratedCvVersions.Add(version);
        db.CvCompileJobs.AddRange(
            CreateCompletedJob("demo:first", Now.AddMinutes(-30)),
            CreateCompletedJob("demo:second", Now.AddMinutes(-1)));
        await db.SaveChangesAsync();
        var queue = new InspectingCompilationQueue(db);
        var service = CreateService(db, queue, maximumDemoJobs: 2);

        var result = await service.RequestAsync(
            workspace,
            UserMode.Demo,
            version.Id,
            CancellationToken.None);

        Assert.Equal(RequestCvCompilationOutcome.QuotaExceeded, result.Outcome);
        Assert.Null(queue.EnqueuedJobId);
        Assert.Equal(2, await db.CvCompileJobs.CountAsync());
    }

    [Fact]
    public async Task Demo_request_ignores_compilations_outside_the_shared_window()
    {
        await using var db = CreateDbContext();
        const string workspace = "demo:current";
        var version = CreateGeneratedVersion(workspace);
        db.GeneratedCvVersions.Add(version);
        db.CvCompileJobs.AddRange(
            CreateCompletedJob("demo:old", Now.AddMinutes(-61)),
            CreateCompletedJob("demo:recent", Now.AddMinutes(-1)));
        await db.SaveChangesAsync();
        var queue = new InspectingCompilationQueue(db);
        var service = CreateService(db, queue, maximumDemoJobs: 2);

        var result = await service.RequestAsync(
            workspace,
            UserMode.Demo,
            version.Id,
            CancellationToken.None);

        Assert.Equal(RequestCvCompilationOutcome.Accepted, result.Outcome);
        Assert.NotNull(queue.EnqueuedJobId);
        Assert.Equal(3, await db.CvCompileJobs.CountAsync());
    }

    [Fact]
    public async Task Real_user_request_is_not_limited_by_demo_compilation_window()
    {
        await using var db = CreateDbContext();
        const string workspace = "user:owner";
        var version = CreateGeneratedVersion(workspace);
        db.GeneratedCvVersions.Add(version);
        db.CvCompileJobs.AddRange(
            CreateCompletedJob("demo:first", Now.AddMinutes(-30)),
            CreateCompletedJob("demo:second", Now.AddMinutes(-1)));
        await db.SaveChangesAsync();
        var queue = new InspectingCompilationQueue(db);
        var service = CreateService(db, queue, maximumDemoJobs: 2);

        var result = await service.RequestAsync(
            workspace,
            UserMode.Real,
            version.Id,
            CancellationToken.None);

        Assert.Equal(RequestCvCompilationOutcome.Accepted, result.Outcome);
        Assert.NotNull(queue.EnqueuedJobId);
        Assert.Equal(3, await db.CvCompileJobs.CountAsync());
    }

    private static CvCompilationService CreateService(
        AppDbContext db,
        ICvCompilationQueue queue,
        int maximumDemoJobs = 2) =>
        new(
            new CvCompilationStore(db),
            queue,
            new CvWorkflowSettings(10, 10, 5_000, 20_000, "test"),
            new DemoSettings(6, maximumDemoJobs, 60),
            new TestTimeProvider(Now));

    private static GeneratedCvVersion CreateGeneratedVersion(string workspace) => new()
    {
        WorkspaceKey = workspace,
        GeneratedCvId = Guid.NewGuid(),
        Version = 1,
        Tex = "\\documentclass{article}",
        Origin = "test"
    };

    private static CvCompileJob CreateCompletedJob(
        string workspace,
        DateTimeOffset createdAt) => new()
    {
        WorkspaceKey = workspace,
        GeneratedCvVersionId = Guid.NewGuid(),
        Status = JobStatus.Succeeded,
        CreatedAt = createdAt
    };

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

    private sealed class TestTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
