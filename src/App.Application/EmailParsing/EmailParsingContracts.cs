namespace App.Application;

public sealed record EmailMessage(
    string ExternalId,
    string Sender,
    string Subject,
    string HtmlBody,
    DateTimeOffset ReceivedAt,
    string? TextBody = null);

public sealed record ParseResult(
    string SourceKey,
    string SourceExternalId,
    string DisplayTitle,
    Dictionary<string, object?> ParsedData,
    Dictionary<string, object?> SearchData);

public enum ParserMatch
{
    Matched,
    Unsupported,
    Ambiguous
}

public sealed record ParserSelection(ParserMatch Match, ISourceParser? Parser);

public interface ISourceParser
{
    string Key { get; }
    int Version { get; }
    bool CanParse(EmailMessage email);
    Task<IReadOnlyList<ParseResult>> ParseAsync(
        EmailMessage email,
        CancellationToken cancellationToken);
}

public interface ISourceParserRegistry
{
    ParserSelection Select(EmailMessage email);
}
