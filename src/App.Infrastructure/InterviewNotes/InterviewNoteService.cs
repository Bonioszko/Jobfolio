using App.Application;
using App.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure;

public sealed class InterviewNoteService(
    AppDbContext db,
    TimeProvider timeProvider) : IInterviewNoteService
{
    private const int MaximumStageLength = 100;
    private const int MaximumNotesLength = 10_000;

    public async Task<IReadOnlyList<InterviewNoteView>?> ListAsync(
        string workspaceKey,
        Guid jobPostingId,
        CancellationToken cancellationToken)
    {
        var jobExists = await JobExistsAsync(workspaceKey, jobPostingId, cancellationToken);
        if (!jobExists) return null;

        return await db.InterviewNotes
            .AsNoTracking()
            .Where(note =>
                note.WorkspaceKey == workspaceKey &&
                note.JobPostingId == jobPostingId)
            .OrderByDescending(note => note.InterviewDate)
            .ThenByDescending(note => note.CreatedAt)
            .Select(note => Map(note))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<CreateInterviewNoteResult> CreateAsync(
        string workspaceKey,
        Guid jobPostingId,
        CreateInterviewNote request,
        CancellationToken cancellationToken)
    {
        var stage = request.Stage?.Trim();
        var notes = request.Notes?.Trim();
        if (string.IsNullOrWhiteSpace(stage) ||
            stage.Length > MaximumStageLength ||
            request.InterviewDate == default ||
            string.IsNullOrWhiteSpace(notes) ||
            notes.Length > MaximumNotesLength)
        {
            return new CreateInterviewNoteResult(CreateInterviewNoteOutcome.InvalidInput);
        }

        var jobExists = await JobExistsAsync(workspaceKey, jobPostingId, cancellationToken);
        if (!jobExists)
        {
            return new CreateInterviewNoteResult(CreateInterviewNoteOutcome.JobPostingNotFound);
        }

        var note = new InterviewNote
        {
            WorkspaceKey = workspaceKey,
            JobPostingId = jobPostingId,
            Stage = stage,
            InterviewDate = request.InterviewDate,
            Notes = notes,
            CreatedAt = timeProvider.GetUtcNow()
        };
        db.InterviewNotes.Add(note);
        await db.SaveChangesAsync(cancellationToken);

        return new CreateInterviewNoteResult(
            CreateInterviewNoteOutcome.Created,
            Map(note));
    }

    private Task<bool> JobExistsAsync(
        string workspaceKey,
        Guid jobPostingId,
        CancellationToken cancellationToken) =>
        db.JobPostings.AnyAsync(
            posting =>
                posting.WorkspaceKey == workspaceKey &&
                posting.Id == jobPostingId,
            cancellationToken);

    private static InterviewNoteView Map(InterviewNote note) =>
        new(
            note.Id,
            note.JobPostingId,
            note.Stage,
            note.InterviewDate,
            note.Notes,
            note.CreatedAt);
}
