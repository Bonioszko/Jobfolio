using App.Application;
using App.Domain;
using App.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace App.Tests;

public sealed class InterviewNoteServiceTests
{
    [Fact]
    public async Task Creates_and_lists_multiple_interview_stages_in_date_order()
    {
        await using var db = CreateDbContext();
        var posting = CreateJobPosting("demo:owner");
        db.JobPostings.Add(posting);
        await db.SaveChangesAsync();
        var service = new InterviewNoteService(db, TimeProvider.System);

        var first = await service.CreateAsync(
            posting.WorkspaceKey,
            posting.Id,
            new CreateInterviewNote("Recruiter screen", new DateOnly(2026, 9, 1), "Good introduction."),
            CancellationToken.None);
        var second = await service.CreateAsync(
            posting.WorkspaceKey,
            posting.Id,
            new CreateInterviewNote("Technical interview", new DateOnly(2026, 9, 5), "Discussed system design."),
            CancellationToken.None);
        var notes = await service.ListAsync(
            posting.WorkspaceKey,
            posting.Id,
            CancellationToken.None);

        Assert.Equal(CreateInterviewNoteOutcome.Created, first.Outcome);
        Assert.Equal(CreateInterviewNoteOutcome.Created, second.Outcome);
        Assert.Collection(
            Assert.IsAssignableFrom<IReadOnlyList<InterviewNoteView>>(notes),
            note => Assert.Equal("Technical interview", note.Stage),
            note => Assert.Equal("Recruiter screen", note.Stage));
    }

    [Fact]
    public async Task Does_not_reveal_or_add_notes_for_another_workspace_job()
    {
        await using var db = CreateDbContext();
        var posting = CreateJobPosting("demo:owner");
        db.JobPostings.Add(posting);
        await db.SaveChangesAsync();
        var service = new InterviewNoteService(db, TimeProvider.System);

        var listed = await service.ListAsync(
            "demo:other",
            posting.Id,
            CancellationToken.None);
        var created = await service.CreateAsync(
            "demo:other",
            posting.Id,
            new CreateInterviewNote("Technical", new DateOnly(2026, 9, 5), "Private note"),
            CancellationToken.None);

        Assert.Null(listed);
        Assert.Equal(CreateInterviewNoteOutcome.JobPostingNotFound, created.Outcome);
        Assert.Empty(db.InterviewNotes);
    }

    [Theory]
    [InlineData("", "Notes")]
    [InlineData("Technical", "")]
    public async Task Rejects_incomplete_interview_notes(string stage, string notes)
    {
        await using var db = CreateDbContext();
        var posting = CreateJobPosting("demo:owner");
        db.JobPostings.Add(posting);
        await db.SaveChangesAsync();
        var service = new InterviewNoteService(db, TimeProvider.System);

        var result = await service.CreateAsync(
            posting.WorkspaceKey,
            posting.Id,
            new CreateInterviewNote(stage, new DateOnly(2026, 9, 5), notes),
            CancellationToken.None);

        Assert.Equal(CreateInterviewNoteOutcome.InvalidInput, result.Outcome);
        Assert.Empty(db.InterviewNotes);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static JobPosting CreateJobPosting(string workspaceKey) => new()
    {
        WorkspaceKey = workspaceKey,
        ProviderKey = "linkedin",
        ProviderExternalId = Guid.NewGuid().ToString(),
        Title = "Backend Engineer",
        NormalizedDataJson = "{}",
        SearchDataJson = "{}",
        ApplicationStatus = "NEW",
        ParserKey = "linkedin",
        ParserVersion = 1,
        SourceReceivedAt = DateTimeOffset.UtcNow
    };
}
