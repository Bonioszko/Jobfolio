using App.Api.Authentication;
using App.Application;

namespace App.Api.Endpoints.CvGeneration;

public static class CvGenerationEndpoints
{
    public static RouteGroupBuilder MapCvGenerationEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/generation-jobs", RequestAsync);
        group.MapGet("/generation-jobs/{id:guid}", GetAsync);
        return group;
    }

    private static async Task<IResult> RequestAsync(
        RequestBody body,
        ICurrentWorkspaceAccessor workspaceAccessor,
        ICvGenerationService cvGeneration,
        CancellationToken cancellationToken)
    {
        var result = await cvGeneration.RequestAsync(
            workspaceAccessor.GetRequired().Key,
            new RequestCvGeneration(
                body.SourceItemId,
                body.TemplateVersionId,
                body.RuleVersionId,
                body.Instruction),
            cancellationToken);

        return result.Outcome switch
        {
            RequestCvGenerationOutcome.Accepted => Results.Accepted(
                $"/api/generation-jobs/{result.Job!.Id}",
                result.Job),
            RequestCvGenerationOutcome.InvalidInput => Results.BadRequest(
                "Invalid CV generation inputs."),
            RequestCvGenerationOutcome.QuotaExceeded => Results.Problem(
                statusCode: StatusCodes.Status429TooManyRequests,
                title: "CV generation quota exceeded."),
            _ => throw new ArgumentOutOfRangeException(nameof(result.Outcome), result.Outcome, null)
        };
    }

    private static async Task<IResult> GetAsync(
        Guid id,
        ICurrentWorkspaceAccessor workspaceAccessor,
        ICvGenerationService cvGeneration,
        CancellationToken cancellationToken)
    {
        var job = await cvGeneration.GetAsync(
            workspaceAccessor.GetRequired().Key,
            id,
            cancellationToken);
        return job is null ? Results.NotFound() : Results.Ok(job);
    }

    private sealed record RequestBody(
        Guid SourceItemId,
        Guid TemplateVersionId,
        Guid RuleVersionId,
        string? Instruction);
}
