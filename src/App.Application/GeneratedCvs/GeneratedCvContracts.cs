namespace App.Application;

public sealed record GeneratedCvView(
    Guid Id,
    int Version,
    Guid VersionId,
    string Tex,
    string Origin);

public interface IGeneratedCvQueryService
{
    Task<GeneratedCvView?> GetAsync(
        string workspaceKey,
        Guid id,
        CancellationToken cancellationToken);
}
