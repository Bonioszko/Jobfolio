using App.Domain;
using App.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace App.Tests;

public sealed class ActiveJobQuotaTests
{
    [Theory]
    [InlineData(JobStatus.Succeeded)]
    [InlineData(JobStatus.Failed)]
    [InlineData(JobStatus.TimedOut)]
    public async Task Generation_quota_ignores_terminal_jobs(JobStatus terminalStatus)
    {
        await using var db = CreateDbContext();
        const string workspace = "demo:generation-quota";
        db.CvGenerationJobs.Add(CreateGenerationJob(workspace, terminalStatus));
        await db.SaveChangesAsync();

        var added = await new CvGenerationStore(db).TryAddAsync(
            CreateGenerationJob(workspace),
            maximumJobsPerWorkspace: 1,
            CancellationToken.None);

        Assert.True(added);
        Assert.Equal(2, await db.CvGenerationJobs.CountAsync());
    }

    [Theory]
    [InlineData(JobStatus.Queued)]
    [InlineData(JobStatus.Running)]
    public async Task Generation_quota_counts_active_jobs(JobStatus activeStatus)
    {
        await using var db = CreateDbContext();
        const string workspace = "demo:generation-quota";
        db.CvGenerationJobs.Add(CreateGenerationJob(workspace, activeStatus));
        await db.SaveChangesAsync();

        var added = await new CvGenerationStore(db).TryAddAsync(
            CreateGenerationJob(workspace),
            maximumJobsPerWorkspace: 1,
            CancellationToken.None);

        Assert.False(added);
        Assert.Single(db.CvGenerationJobs);
    }

    [Theory]
    [InlineData(JobStatus.Succeeded)]
    [InlineData(JobStatus.Failed)]
    [InlineData(JobStatus.TimedOut)]
    public async Task Compilation_quota_ignores_terminal_jobs(JobStatus terminalStatus)
    {
        await using var db = CreateDbContext();
        const string workspace = "demo:compilation-quota";
        db.CvCompileJobs.Add(CreateCompilationJob(workspace, terminalStatus));
        await db.SaveChangesAsync();

        var added = await new CvCompilationStore(db).TryAddAsync(
            CreateCompilationJob(workspace),
            maximumJobsPerWorkspace: 1,
            demoQuota: null,
            CancellationToken.None);

        Assert.True(added);
        Assert.Equal(2, await db.CvCompileJobs.CountAsync());
    }

    [Theory]
    [InlineData(JobStatus.Queued)]
    [InlineData(JobStatus.Running)]
    public async Task Compilation_quota_counts_active_jobs(JobStatus activeStatus)
    {
        await using var db = CreateDbContext();
        const string workspace = "demo:compilation-quota";
        db.CvCompileJobs.Add(CreateCompilationJob(workspace, activeStatus));
        await db.SaveChangesAsync();

        var added = await new CvCompilationStore(db).TryAddAsync(
            CreateCompilationJob(workspace),
            maximumJobsPerWorkspace: 1,
            demoQuota: null,
            CancellationToken.None);

        Assert.False(added);
        Assert.Single(db.CvCompileJobs);
    }

    [Fact]
    public async Task Compilation_rejects_a_job_without_exactly_one_source()
    {
        await using var db = CreateDbContext();
        var store = new CvCompilationStore(db);

        await Assert.ThrowsAsync<ArgumentException>(() => store.TryAddAsync(
            new CvCompileJob { WorkspaceKey = "demo:invalid" },
            maximumJobsPerWorkspace: 1,
            demoQuota: null,
            CancellationToken.None));

        await Assert.ThrowsAsync<ArgumentException>(() => store.TryAddAsync(
            new CvCompileJob
            {
                WorkspaceKey = "demo:invalid",
                GeneratedCvVersionId = Guid.NewGuid(),
                CvTemplateVersionId = Guid.NewGuid()
            },
            maximumJobsPerWorkspace: 1,
            demoQuota: null,
            CancellationToken.None));
    }

    private static CvGenerationJob CreateGenerationJob(
        string workspace,
        JobStatus status = JobStatus.Queued) => new()
    {
        WorkspaceKey = workspace,
        JobPostingSnapshotJson = "{}",
        SystemRulesHash = "rules-hash",
        Model = "test",
        Status = status
    };

    private static CvCompileJob CreateCompilationJob(
        string workspace,
        JobStatus status = JobStatus.Queued) => new()
    {
        WorkspaceKey = workspace,
        GeneratedCvVersionId = Guid.NewGuid(),
        Status = status
    };

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(
                InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }
}
