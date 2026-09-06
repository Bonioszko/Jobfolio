namespace App.Domain;

public sealed class InterviewNote : WorkspaceOwnedEntity
{
    public Guid JobPostingId { get; set; }
    public required string Stage { get; set; }
    public DateOnly InterviewDate { get; set; }
    public required string Notes { get; set; }
}
