namespace App.Domain;

public sealed class GmailMessageReceipt : WorkspaceOwnedEntity
{
    public required string GmailMessageId { get; set; }
    public required string ProcessingStatus { get; set; }
    public string? ParserKey { get; set; }
    public int? ParserVersion { get; set; }
    public DateTimeOffset SourceReceivedAt { get; set; }
}
