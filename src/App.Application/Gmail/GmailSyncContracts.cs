namespace App.Application;

public sealed record GmailSyncSettings(
    bool Enabled,
    bool RunOnce,
    string WorkspaceKey,
    IReadOnlyList<string> Labels,
    TimeSpan PollInterval,
    int MaxMessagesPerRun,
    int MaxBodyBytes);

public interface IEmailProvider
{
    Task<IReadOnlyList<string>> ListMessageIdsByLabelsAsync(
        IReadOnlyList<string> labels,
        int maxMessages,
        CancellationToken cancellationToken);

    Task<EmailMessage> GetMessageAsync(
        string messageId,
        int maxBodyBytes,
        CancellationToken cancellationToken);
}

public enum GmailMessageProcessingOutcome
{
    Imported,
    Unsupported,
    Ambiguous,
    AlreadyProcessed
}

public interface IGmailMessageProcessor
{
    Task<GmailMessageProcessingOutcome> ProcessAsync(
        string workspaceKey,
        EmailMessage email,
        CancellationToken cancellationToken);
}

public interface IGmailImportStore
{
    Task<bool> IsProcessedAsync(
        string workspaceKey,
        string gmailMessageId,
        CancellationToken cancellationToken);

    Task SaveAsync(
        string workspaceKey,
        EmailMessage email,
        string processingStatus,
        string? parserKey,
        int? parserVersion,
        IReadOnlyList<ParseResult> postings,
        CancellationToken cancellationToken);
}
