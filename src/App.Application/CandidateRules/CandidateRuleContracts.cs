namespace App.Application;

public sealed record CandidateRuleView(Guid Id, int Version, Guid VersionId, string Markdown);

public interface ICandidateRuleQueryService
{
    Task<CandidateRuleView> GetCurrentAsync(
        string workspaceKey,
        CancellationToken cancellationToken);
}
