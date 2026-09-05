using App.Api.Authentication;
using App.Application;

namespace App.Api.Endpoints.JobPostings;

public static class JobPostingEndpoints
{
    public static RouteGroupBuilder MapJobPostingEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/source-items", ListAsync);
        group.MapGet("/source-items/{id:guid}", GetAsync);
        group.MapPut("/source-items/{id:guid}/status", ChangeStatusAsync);
        return group;
    }

    private static async Task<IResult> ListAsync(
        ICurrentWorkspaceAccessor workspaceAccessor,
        IJobPostingQueryService jobPostings,
        string? status,
        string? source,
        int? limit,
        CancellationToken cancellationToken)
    {
        var workspace = workspaceAccessor.GetRequired();
        var result = await jobPostings.ListAsync(
            workspace.Key,
            status,
            source,
            limit ?? 50,
            cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetAsync(
        Guid id,
        ICurrentWorkspaceAccessor workspaceAccessor,
        IJobPostingQueryService jobPostings,
        CancellationToken cancellationToken)
    {
        var result = await jobPostings.GetAsync(
            workspaceAccessor.GetRequired().Key,
            id,
            cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> ChangeStatusAsync(
        Guid id,
        ChangeStatusRequest request,
        ICurrentWorkspaceAccessor workspaceAccessor,
        IApplicationStatusService applicationStatus,
        CancellationToken cancellationToken)
    {
        var outcome = await applicationStatus.ChangeAsync(
            workspaceAccessor.GetRequired().Key,
            id,
            request.Status,
            cancellationToken);

        return outcome switch
        {
            ChangeApplicationStatusOutcome.Updated => Results.NoContent(),
            ChangeApplicationStatusOutcome.NotFound => Results.NotFound(),
            ChangeApplicationStatusOutcome.InvalidStatus => Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["status"] = ["Unknown workflow status."]
                }),
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null)
        };
    }

    private sealed record ChangeStatusRequest(string Status);
}
