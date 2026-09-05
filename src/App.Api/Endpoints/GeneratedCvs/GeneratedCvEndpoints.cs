using App.Api.Authentication;
using App.Application;

namespace App.Api.Endpoints.GeneratedCvs;

public static class GeneratedCvEndpoints
{
    public static RouteGroupBuilder MapGeneratedCvEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/documents/{id:guid}", GetAsync);
        return group;
    }

    private static async Task<IResult> GetAsync(
        Guid id,
        ICurrentWorkspaceAccessor workspaceAccessor,
        IGeneratedCvQueryService generatedCvs,
        CancellationToken cancellationToken)
    {
        var generatedCv = await generatedCvs.GetAsync(
            workspaceAccessor.GetRequired().Key,
            id,
            cancellationToken);
        return generatedCv is null ? Results.NotFound() : Results.Ok(generatedCv);
    }
}
