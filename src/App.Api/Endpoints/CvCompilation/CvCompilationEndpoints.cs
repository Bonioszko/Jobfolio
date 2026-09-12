using App.Api.Authentication;
using App.Application;

namespace App.Api.Endpoints.CvCompilation;

public static class CvCompilationEndpoints
{
    public static RouteGroupBuilder MapCvCompilationEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/compile-jobs", RequestAsync);
        group.MapPost("/template-compile-jobs", RequestTemplateAsync);
        group.MapGet("/compile-jobs/{id:guid}", GetAsync);
        return group;
    }

    private static async Task<IResult> RequestTemplateAsync(
        RequestBody body,
        ICurrentWorkspaceAccessor workspaceAccessor,
        ICvCompilationService cvCompilation,
        CancellationToken cancellationToken)
    {
        var result = await cvCompilation.RequestTemplateAsync(
            workspaceAccessor.GetRequired().Key,
            body.DocumentVersionId,
            cancellationToken);

        return result.Outcome switch
        {
            RequestCvCompilationOutcome.Accepted => Results.Accepted(
                $"/api/compile-jobs/{result.Job!.Id}", result.Job),
            RequestCvCompilationOutcome.InvalidInput => Results.BadRequest("Invalid CV template version."),
            RequestCvCompilationOutcome.QuotaExceeded => Results.Problem(
                statusCode: StatusCodes.Status429TooManyRequests,
                title: "CV compilation quota exceeded."),
            _ => throw new ArgumentOutOfRangeException(nameof(result.Outcome), result.Outcome, null)
        };
    }

    private static async Task<IResult> RequestAsync(
        RequestBody body,
        ICurrentWorkspaceAccessor workspaceAccessor,
        ICvCompilationService cvCompilation,
        CancellationToken cancellationToken)
    {
        var result = await cvCompilation.RequestAsync(
            workspaceAccessor.GetRequired().Key,
            body.DocumentVersionId,
            cancellationToken);

        return result.Outcome switch
        {
            RequestCvCompilationOutcome.Accepted => Results.Accepted(
                $"/api/compile-jobs/{result.Job!.Id}",
                result.Job),
            RequestCvCompilationOutcome.InvalidInput => Results.BadRequest(
                "Invalid generated CV version."),
            RequestCvCompilationOutcome.QuotaExceeded => Results.Problem(
                statusCode: StatusCodes.Status429TooManyRequests,
                title: "CV compilation quota exceeded."),
            _ => throw new ArgumentOutOfRangeException(nameof(result.Outcome), result.Outcome, null)
        };
    }

    private static async Task<IResult> GetAsync(
        Guid id,
        ICurrentWorkspaceAccessor workspaceAccessor,
        ICvCompilationService cvCompilation,
        CancellationToken cancellationToken)
    {
        var job = await cvCompilation.GetAsync(
            workspaceAccessor.GetRequired().Key,
            id,
            cancellationToken);
        return job is null ? Results.NotFound() : Results.Ok(job);
    }

    private sealed record RequestBody(Guid DocumentVersionId);
}
