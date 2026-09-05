using App.Api.Authentication;
using App.Application;

namespace App.Api.Endpoints.CvTemplates;

public static class CvTemplateEndpoints
{
    public static RouteGroupBuilder MapCvTemplateEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet(
            "/templates",
            async (
                ICurrentWorkspaceAccessor workspaceAccessor,
                ICvTemplateQueryService templates,
                CancellationToken cancellationToken) =>
                Results.Ok(await templates.ListAsync(
                    workspaceAccessor.GetRequired().Key,
                    cancellationToken)));
        return group;
    }
}
