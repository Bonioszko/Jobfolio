using App.Api.Authentication;
using App.Application;

namespace App.Api.Endpoints.InterviewNotes;

public static class InterviewNoteEndpoints
{
    public static RouteGroupBuilder MapInterviewNoteEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/source-items/{jobPostingId:guid}/interview-notes", ListAsync);
        group.MapPost("/source-items/{jobPostingId:guid}/interview-notes", CreateAsync);
        return group;
    }

    private static async Task<IResult> ListAsync(
        Guid jobPostingId,
        ICurrentWorkspaceAccessor workspaceAccessor,
        IInterviewNoteService interviewNotes,
        CancellationToken cancellationToken)
    {
        var notes = await interviewNotes.ListAsync(
            workspaceAccessor.GetRequired().Key,
            jobPostingId,
            cancellationToken);
        return notes is null ? Results.NotFound() : Results.Ok(notes);
    }

    private static async Task<IResult> CreateAsync(
        Guid jobPostingId,
        CreateInterviewNote request,
        ICurrentWorkspaceAccessor workspaceAccessor,
        IInterviewNoteService interviewNotes,
        CancellationToken cancellationToken)
    {
        var result = await interviewNotes.CreateAsync(
            workspaceAccessor.GetRequired().Key,
            jobPostingId,
            request,
            cancellationToken);

        return result.Outcome switch
        {
            CreateInterviewNoteOutcome.Created => Results.Created(
                $"/api/source-items/{jobPostingId}/interview-notes/{result.Note!.Id}",
                result.Note),
            CreateInterviewNoteOutcome.InvalidInput => Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["interviewNote"] =
                    ["Stage, interview date, and notes are required and must be within their size limits."]
                }),
            CreateInterviewNoteOutcome.JobPostingNotFound => Results.NotFound(),
            _ => throw new ArgumentOutOfRangeException(nameof(result.Outcome), result.Outcome, null)
        };
    }
}
