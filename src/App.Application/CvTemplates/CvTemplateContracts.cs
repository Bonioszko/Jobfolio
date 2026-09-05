namespace App.Application;

public sealed record CvTemplateView(Guid Id, string Name, int Version, Guid VersionId, string Tex);

public interface ICvTemplateQueryService
{
    Task<IReadOnlyList<CvTemplateView>> ListAsync(
        string workspaceKey,
        CancellationToken cancellationToken);
}
