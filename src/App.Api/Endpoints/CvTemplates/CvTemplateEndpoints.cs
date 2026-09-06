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
        group.MapPost("/templates", CreateAsync);
        group.MapPut("/templates/{id:guid}", UpdateAsync);
        return group;
    }

    private static async Task<IResult> CreateAsync(
        SaveCvTemplate request,
        ICurrentWorkspaceAccessor workspaceAccessor,
        ICvTemplateCommandService templates,
        CancellationToken cancellationToken)
    {
        var result = await templates.CreateAsync(
            workspaceAccessor.GetRequired().Key,
            request,
            cancellationToken);
        return MapSaveResult(result, isCreate: true);
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        SaveCvTemplate request,
        ICurrentWorkspaceAccessor workspaceAccessor,
        ICvTemplateCommandService templates,
        CancellationToken cancellationToken)
    {
        var result = await templates.UpdateAsync(
            workspaceAccessor.GetRequired().Key,
            id,
            request,
            cancellationToken);
        return MapSaveResult(result, isCreate: false);
    }

    private static IResult MapSaveResult(SaveCvTemplateResult result, bool isCreate) =>
        result.Outcome switch
        {
            SaveCvTemplateOutcome.Saved when isCreate => Results.Created(
                $"/api/templates/{result.Template!.Id}",
                result.Template),
            SaveCvTemplateOutcome.Saved => Results.Ok(result.Template),
            SaveCvTemplateOutcome.InvalidInput => Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["template"] = ["A name and safe, non-empty TeX document are required."]
                }),
            SaveCvTemplateOutcome.NotFound => Results.NotFound(),
            SaveCvTemplateOutcome.Conflict => Results.Conflict(
                new { message = "A CV template with this name already exists." }),
            SaveCvTemplateOutcome.LimitReached => Results.Conflict(
                new { message = "The workspace CV template limit has been reached." }),
            _ => throw new ArgumentOutOfRangeException(nameof(result), result.Outcome, null)
        };
}
