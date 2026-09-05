namespace App.Domain;

public sealed class CvTemplate : WorkspaceOwnedEntity
{
    public required string Name { get; set; }
    public int CurrentVersion { get; set; }
    public bool IsArchived { get; set; }
}

public sealed class CvTemplateVersion : WorkspaceOwnedEntity
{
    public Guid CvTemplateId { get; set; }
    public int Version { get; set; }
    public required string Tex { get; set; }
}
