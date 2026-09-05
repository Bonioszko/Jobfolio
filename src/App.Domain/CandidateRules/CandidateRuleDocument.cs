namespace App.Domain;

public sealed class CandidateRuleDocument : WorkspaceOwnedEntity
{
    public int CurrentVersion { get; set; }
}

public sealed class CandidateRuleVersion : WorkspaceOwnedEntity
{
    public Guid CandidateRuleDocumentId { get; set; }
    public int Version { get; set; }
    public required string Markdown { get; set; }
}
