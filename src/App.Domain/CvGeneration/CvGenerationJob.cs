namespace App.Domain;

public sealed class CvGenerationJob : LeasedJob
{
    public Guid JobPostingId { get; set; }
    public Guid CvTemplateVersionId { get; set; }
    public Guid CandidateRuleVersionId { get; set; }
    public required string JobPostingSnapshotJson { get; set; }
    public required string SystemRulesHash { get; set; }
    public required string Model { get; set; }
    public string? UserInstruction { get; set; }
    public Guid? GeneratedCvId { get; set; }
}

public sealed class GeneratedCv : WorkspaceOwnedEntity
{
    public Guid JobPostingId { get; set; }
    public int CurrentVersion { get; set; }
}

public sealed class GeneratedCvVersion : WorkspaceOwnedEntity
{
    public Guid GeneratedCvId { get; set; }
    public int Version { get; set; }
    public required string Tex { get; set; }
    public required string Origin { get; set; }
}
