namespace App.Domain;

public abstract class WorkspaceOwnedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string WorkspaceKey { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
