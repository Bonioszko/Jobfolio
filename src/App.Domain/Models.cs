using System.Text.Json;

namespace App.Domain;

public enum UserMode { Real, Demo }
public enum JobStatus { Queued, Running, Succeeded, Failed, TimedOut }

public sealed record CurrentUser(string UserId, UserMode Mode, string? Email, string? DemoSessionId)
{
    public string WorkspaceKey => Mode == UserMode.Demo
        ? $"demo:{DemoSessionId ?? throw new InvalidOperationException("Missing demo session")}" : $"user:{UserId}";
}

public abstract class WorkspaceEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string WorkspaceKey { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class DemoSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }
}

public sealed class SourceItem : WorkspaceEntity
{
    public required string SourceKey { get; set; }
    public string? SourceExternalId { get; set; }
    public required string DisplayTitle { get; set; }
    public required string ParsedDataJson { get; set; }
    public required string SearchDataJson { get; set; }
    public required string WorkflowStatus { get; set; }
    public required string ParserKey { get; set; }
    public int ParserVersion { get; set; }
    public DateTimeOffset SourceReceivedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? DemoEmailHtml { get; set; }
}

public sealed class StatusHistory : WorkspaceEntity
{
    public Guid SourceItemId { get; set; }
    public required string PreviousStatus { get; set; }
    public required string NewStatus { get; set; }
}

public sealed class DocumentTemplate : WorkspaceEntity
{
    public required string Name { get; set; }
    public int CurrentVersion { get; set; }
    public bool IsArchived { get; set; }
}

public sealed class DocumentTemplateVersion : WorkspaceEntity
{
    public Guid TemplateId { get; set; }
    public int Version { get; set; }
    public required string Tex { get; set; }
}

public sealed class UserRuleDocument : WorkspaceEntity
{
    public int CurrentVersion { get; set; }
}

public sealed class UserRuleVersion : WorkspaceEntity
{
    public Guid RuleDocumentId { get; set; }
    public int Version { get; set; }
    public required string Markdown { get; set; }
}

public abstract class LeasedJob : WorkspaceEntity
{
    public JobStatus Status { get; set; } = JobStatus.Queued;
    public DateTimeOffset? LeaseUntil { get; set; }
    public int AttemptCount { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

public sealed class GenerationJob : LeasedJob
{
    public Guid SourceItemId { get; set; }
    public Guid TemplateVersionId { get; set; }
    public Guid RuleVersionId { get; set; }
    public required string SourceSnapshotJson { get; set; }
    public required string SystemRulesHash { get; set; }
    public required string Model { get; set; }
    public string? UserInstruction { get; set; }
    public Guid? GeneratedDocumentId { get; set; }
}

public sealed class GeneratedDocument : WorkspaceEntity
{
    public Guid SourceItemId { get; set; }
    public int CurrentVersion { get; set; }
}

public sealed class GeneratedDocumentVersion : WorkspaceEntity
{
    public Guid DocumentId { get; set; }
    public int Version { get; set; }
    public required string Tex { get; set; }
    public required string Origin { get; set; }
}

public sealed class CompileJob : LeasedJob
{
    public Guid DocumentVersionId { get; set; }
    public Guid? PdfArtifactId { get; set; }
}

public sealed class PdfArtifact : WorkspaceEntity
{
    public Guid DocumentVersionId { get; set; }
    public required string ObjectKey { get; set; }
    public required string Sha256 { get; set; }
    public long Size { get; set; }
}

public static class JsonDefaults
{
    public static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
}
