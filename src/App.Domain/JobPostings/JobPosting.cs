namespace App.Domain;

public sealed class JobPosting : WorkspaceOwnedEntity
{
    public required string ProviderKey { get; set; }
    public string? ProviderExternalId { get; set; }
    public required string Title { get; set; }
    public required string NormalizedDataJson { get; set; }
    public required string SearchDataJson { get; set; }
    public required string ApplicationStatus { get; set; }
    public required string ParserKey { get; set; }
    public int ParserVersion { get; set; }
    public DateTimeOffset SourceReceivedAt { get; set; }
    public DateTimeOffset? AppliedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? DemoEmailHtml { get; set; }
}

public sealed class ApplicationStatusHistory : WorkspaceOwnedEntity
{
    public Guid JobPostingId { get; set; }
    public required string PreviousStatus { get; set; }
    public required string NewStatus { get; set; }
}
