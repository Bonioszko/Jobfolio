using App.Domain;

namespace App.Tests;

public sealed class JobStateTests
{
    [Fact]
    public void Queued_job_can_be_claimed_and_completed()
    {
        var now = new DateTimeOffset(2026, 9, 5, 10, 0, 0, TimeSpan.Zero);
        var job = CreateJob();

        job.MarkClaimed(now, TimeSpan.FromMinutes(2));
        job.MarkSucceeded(now.AddSeconds(10));

        Assert.Equal(JobStatus.Succeeded, job.Status);
        Assert.Equal(1, job.AttemptCount);
        Assert.Null(job.LeaseUntil);
        Assert.Equal(now.AddSeconds(10), job.CompletedAt);
    }

    [Fact]
    public void Active_lease_cannot_be_claimed_again()
    {
        var now = new DateTimeOffset(2026, 9, 5, 10, 0, 0, TimeSpan.Zero);
        var job = CreateJob();
        job.MarkClaimed(now, TimeSpan.FromMinutes(2));

        Assert.Throws<InvalidOperationException>(() =>
            job.MarkClaimed(now.AddMinutes(1), TimeSpan.FromMinutes(2)));
    }

    [Fact]
    public void Expired_lease_can_be_reclaimed()
    {
        var now = new DateTimeOffset(2026, 9, 5, 10, 0, 0, TimeSpan.Zero);
        var job = CreateJob();
        job.MarkClaimed(now, TimeSpan.FromMinutes(1));

        job.MarkClaimed(now.AddMinutes(2), TimeSpan.FromMinutes(1));

        Assert.Equal(JobStatus.Running, job.Status);
        Assert.Equal(2, job.AttemptCount);
        Assert.Equal(now.AddMinutes(3), job.LeaseUntil);
    }

    [Fact]
    public void Terminal_job_cannot_be_reclaimed()
    {
        var now = new DateTimeOffset(2026, 9, 5, 10, 0, 0, TimeSpan.Zero);
        var job = CreateJob();
        job.MarkClaimed(now, TimeSpan.FromMinutes(1));
        job.MarkSucceeded(now.AddSeconds(5));

        Assert.Throws<InvalidOperationException>(() =>
            job.MarkClaimed(now.AddMinutes(2), TimeSpan.FromMinutes(1)));
    }

    private static CvGenerationJob CreateJob() => new()
    {
        WorkspaceKey = "demo:test",
        JobPostingSnapshotJson = "{}",
        SystemRulesHash = "hash",
        Model = "test"
    };
}
