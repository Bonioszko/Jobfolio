using App.Api.Authentication;
using App.Application;

namespace App.Api.Endpoints.Artifacts;

public static class ArtifactEndpoints
{
    public static RouteGroupBuilder MapArtifactEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/pdf-artifacts/{id:guid}/download", DownloadAsync);
        return group;
    }

    private static async Task<IResult> DownloadAsync(
        Guid id,
        ICurrentWorkspaceAccessor workspaceAccessor,
        IPdfArtifactService artifacts,
        CancellationToken cancellationToken)
    {
        var content = await artifacts.OpenReadAsync(
            workspaceAccessor.GetRequired().Key,
            id,
            cancellationToken);

        return content is null
            ? Results.NotFound()
            : Results.File(
                content,
                contentType: "application/pdf",
                fileDownloadName: $"cv-{id:N}.pdf",
                enableRangeProcessing: true);
    }
}
