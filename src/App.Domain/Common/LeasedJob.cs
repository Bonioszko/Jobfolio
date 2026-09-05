namespace App.Domain;

public abstract class LeasedJob : WorkspaceOwnedEntity
{
    private const int MaximumErrorLength = 500;

    public JobStatus Status { get; set; } = JobStatus.Queued;
    public DateTimeOffset? LeaseUntil { get; set; }
    public int AttemptCount { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public void MarkClaimed(DateTimeOffset now, TimeSpan leaseDuration)
    {
        if (leaseDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(leaseDuration));
        }

        var canClaim = Status == JobStatus.Queued ||
                       Status == JobStatus.Running && LeaseUntil <= now;
        if (!canClaim)
        {
            throw new InvalidOperationException($"A {Status} job cannot be claimed.");
        }

        Status = JobStatus.Running;
        AttemptCount++;
        LeaseUntil = now.Add(leaseDuration);
        Error = null;
        CompletedAt = null;
    }

    public void MarkSucceeded(DateTimeOffset completedAt) =>
        MarkTerminal(JobStatus.Succeeded, completedAt, error: null);

    public void MarkFailed(DateTimeOffset completedAt, string error) =>
        MarkTerminal(JobStatus.Failed, completedAt, NormalizeError(error));

    public void MarkTimedOut(DateTimeOffset completedAt, string error) =>
        MarkTerminal(JobStatus.TimedOut, completedAt, NormalizeError(error));

    private void MarkTerminal(JobStatus status, DateTimeOffset completedAt, string? error)
    {
        if (Status != JobStatus.Running)
        {
            throw new InvalidOperationException($"A {Status} job cannot transition to {status}.");
        }

        Status = status;
        CompletedAt = completedAt;
        LeaseUntil = null;
        Error = error;
    }

    private static string NormalizeError(string error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        var trimmed = error.Trim();
        return trimmed[..Math.Min(MaximumErrorLength, trimmed.Length)];
    }
}
