using App.Domain;

namespace App.Application;

public sealed record EmailMessage(string ExternalId, string Sender, string Subject, string HtmlBody, DateTimeOffset ReceivedAt);
public sealed record ParseResult(string SourceKey, string SourceExternalId, string DisplayTitle, Dictionary<string, object?> ParsedData, Dictionary<string, object?> SearchData);
public enum ParserMatch { Matched, Unsupported, Ambiguous }
public sealed record ParserSelection(ParserMatch Match, ISourceParser? Parser);

public interface ISourceParser
{
    string Key { get; }
    int Version { get; }
    bool CanParse(EmailMessage email);
    Task<ParseResult> ParseAsync(EmailMessage email, CancellationToken cancellationToken);
}

public interface ISourceParserRegistry
{
    ParserSelection Select(EmailMessage email);
}

public interface IDemoWorkspaceSeeder
{
    Task SeedAsync(Guid sessionId, CancellationToken cancellationToken);
}

public interface IAiDocumentGenerator
{
    Task<string> GenerateAsync(string sourceSnapshotJson, string template, string rules, string? instruction, CancellationToken cancellationToken);
}

public interface ITexCompiler
{
    Task<byte[]> CompileAsync(string tex, CancellationToken cancellationToken);
}

public interface IArtifactStorage
{
    Task<(string Key, string Sha256, long Size)> SaveAsync(string workspaceKey, Guid artifactId, byte[] data, CancellationToken cancellationToken);
    Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken);
    Task DeleteWorkspaceAsync(string workspaceKey, CancellationToken cancellationToken);
}

public sealed record DomainLabel(string Singular, string Plural);
public sealed record DomainField(string Key, string Label, string Type, bool ShowInDashboard);
public sealed record WorkflowStatusDefinition(string Code, string Label);
public sealed record DomainConfiguration(DomainLabel SourceItem, DomainLabel GeneratedDocument, IReadOnlyList<DomainField> Fields, IReadOnlyList<WorkflowStatusDefinition> Statuses)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(SourceItem.Singular) || string.IsNullOrWhiteSpace(SourceItem.Plural)) throw new InvalidOperationException("Source item labels are required.");
        if (Fields.Count == 0 || Fields.Any(x => string.IsNullOrWhiteSpace(x.Key) || string.IsNullOrWhiteSpace(x.Label))) throw new InvalidOperationException("Domain fields must have keys and labels.");
        if (Fields.Select(x => x.Key).Distinct(StringComparer.OrdinalIgnoreCase).Count() != Fields.Count) throw new InvalidOperationException("Domain field keys must be unique.");
        if (Statuses.Count == 0 || Statuses.Any(x => string.IsNullOrWhiteSpace(x.Code) || string.IsNullOrWhiteSpace(x.Label))) throw new InvalidOperationException("Workflow statuses must have codes and labels.");
        if (Statuses.Select(x => x.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count() != Statuses.Count) throw new InvalidOperationException("Workflow status codes must be unique.");
    }
}

public sealed record SourceItemView(Guid Id, string SourceKey, string DisplayTitle, object? ParsedData, object? SearchData, string WorkflowStatus, string ParserKey, int ParserVersion, DateTimeOffset SourceReceivedAt, string? DemoEmailHtml);

public interface IWorkspaceApplicationService
{
    Task<IReadOnlyList<SourceItemView>> GetSourceItemsAsync(string workspaceKey, string? status, string? source, int limit, CancellationToken cancellationToken);
    Task<SourceItemView?> GetSourceItemAsync(string workspaceKey, Guid id, CancellationToken cancellationToken);
    Task<bool> ChangeStatusAsync(string workspaceKey, Guid id, string status, CancellationToken cancellationToken);
}

public sealed record TemplateView(Guid Id, string Name, int Version, Guid VersionId, string Tex);
public sealed record RuleView(Guid Id, int Version, Guid VersionId, string Markdown);
public sealed record DocumentView(Guid Id, int Version, Guid VersionId, string Tex, string Origin);
public sealed record JobView(Guid Id, JobStatus Status, string? Error, Guid? OutputId);

public interface IGenerationQueue { Task EnqueueAsync(Guid generationJobId, CancellationToken cancellationToken); }
public interface ICompilationQueue { Task EnqueueAsync(Guid compileJobId, CancellationToken cancellationToken); }

public interface IDocumentWorkflowService
{
    Task<IReadOnlyList<TemplateView>> GetTemplatesAsync(string workspaceKey, CancellationToken cancellationToken);
    Task<RuleView> GetRulesAsync(string workspaceKey, CancellationToken cancellationToken);
    Task<JobView?> CreateGenerationAsync(string workspaceKey, Guid sourceItemId, Guid templateVersionId, Guid ruleVersionId, string? instruction, CancellationToken cancellationToken);
    Task<JobView?> GetGenerationAsync(string workspaceKey, Guid id, CancellationToken cancellationToken);
    Task<DocumentView?> GetDocumentAsync(string workspaceKey, Guid id, CancellationToken cancellationToken);
    Task<JobView?> CreateCompileAsync(string workspaceKey, Guid documentVersionId, CancellationToken cancellationToken);
    Task<JobView?> GetCompileAsync(string workspaceKey, Guid id, CancellationToken cancellationToken);
}
