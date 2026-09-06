namespace App.Application;

public sealed record InterviewNoteView(
    Guid Id,
    Guid JobPostingId,
    string Stage,
    DateOnly InterviewDate,
    string Notes,
    DateTimeOffset CreatedAt);

public sealed record CreateInterviewNote(
    string Stage,
    DateOnly InterviewDate,
    string Notes);

public enum CreateInterviewNoteOutcome
{
    Created,
    InvalidInput,
    JobPostingNotFound
}

public sealed record CreateInterviewNoteResult(
    CreateInterviewNoteOutcome Outcome,
    InterviewNoteView? Note = null);

public interface IInterviewNoteService
{
    Task<IReadOnlyList<InterviewNoteView>?> ListAsync(
        string workspaceKey,
        Guid jobPostingId,
        CancellationToken cancellationToken);

    Task<CreateInterviewNoteResult> CreateAsync(
        string workspaceKey,
        Guid jobPostingId,
        CreateInterviewNote request,
        CancellationToken cancellationToken);
}
