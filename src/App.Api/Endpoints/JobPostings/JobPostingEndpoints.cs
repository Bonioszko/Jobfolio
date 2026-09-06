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
        string? cursor,
        int? limit,
        CancellationToken cancellationToken)
    {
        JobPostingCursor? parsedCursor = null;
        if (!string.IsNullOrWhiteSpace(cursor))
        {
            if (!JobPostingCursorCodec.TryDecode(cursor, out var decodedCursor))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["cursor"] = ["The pagination cursor is invalid."]
                });
            }

            parsedCursor = decodedCursor;
        }

        var workspace = workspaceAccessor.GetRequired();
        var page = await jobPostings.ListAsync(
            workspace.Key,
            status,
            source,
            parsedCursor,
            limit ?? 50,
            cancellationToken);
        return Results.Ok(new JobPostingPageResponse(
            page.Items,
            page.NextCursor is null ? null : JobPostingCursorCodec.Encode(page.NextCursor)));
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

    private sealed record JobPostingPageResponse(
        IReadOnlyList<JobPostingView> Items,
        string? NextCursor);
}
