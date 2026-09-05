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
    string? DemoEmailHtml);

public interface IJobPostingQueryService
{
    Task<IReadOnlyList<JobPostingView>> ListAsync(
        string workspaceKey,
        string? status,
        string? source,
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
