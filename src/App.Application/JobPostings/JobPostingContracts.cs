namespace App.Application;

public sealed record JobPostingView(
    Guid Id,
    string SourceKey,
    string DisplayTitle,
    object? ParsedData,
    object? SearchData,
    string WorkflowStatus,
    string ParserKey,
    int ParserVersion,
    DateTimeOffset SourceReceivedAt,
    DateTimeOffset? AppliedAt,
    string? DemoEmailHtml);

public sealed record JobPostingCursor(DateTimeOffset SourceReceivedAt, Guid Id);

public sealed record JobPostingPage(
    IReadOnlyList<JobPostingView> Items,
    JobPostingCursor? NextCursor);

public interface IJobPostingQueryService
{
    Task<JobPostingPage> ListAsync(
        string workspaceKey,
        string? status,
        string? source,
        JobPostingCursor? cursor,
        int limit,
        CancellationToken cancellationToken);

    Task<JobPostingView?> GetAsync(
        string workspaceKey,
        Guid id,
        CancellationToken cancellationToken);
}

public enum ChangeApplicationStatusOutcome
{
    Updated,
    InvalidStatus,
    NotFound
}

public interface IApplicationStatusService
{
    Task<ChangeApplicationStatusOutcome> ChangeAsync(
        string workspaceKey,
        Guid jobPostingId,
        string status,
        CancellationToken cancellationToken);
}

public interface IApplicationStatusStore
{
    Task<bool> ChangeAsync(
        string workspaceKey,
        Guid jobPostingId,
        string status,
        DateTimeOffset changedAt,
        CancellationToken cancellationToken);
}
