using App.Application;
using App.Domain;
using App.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace App.Tests;

public sealed class ApplicationStatusServiceTests
{
    [Fact]
    public async Task Valid_status_change_is_scoped_and_records_history()
    {
        await using var db = CreateDbContext();
        var visible = CreatePosting("demo:visible", "NEW");
        var hidden = CreatePosting("demo:hidden", "NEW");
        db.JobPostings.AddRange(visible, hidden);
        await db.SaveChangesAsync();
        var now = new DateTimeOffset(2026, 9, 5, 12, 0, 0, TimeSpan.Zero);
        var service = new ApplicationStatusService(
            new ApplicationStatusStore(db),
            new StubDomainConfigurationProvider(),
            new FixedTimeProvider(now));

        var outcome = await service.ChangeAsync(
            visible.WorkspaceKey,
            visible.Id,
            "APPLIED",
            CancellationToken.None);

        Assert.Equal(ChangeApplicationStatusOutcome.Updated, outcome);
        Assert.Equal("APPLIED", visible.ApplicationStatus);
        Assert.Equal("NEW", hidden.ApplicationStatus);
        Assert.Equal(now, visible.UpdatedAt);
        var history = await db.ApplicationStatusHistory.SingleAsync();
        Assert.Equal(visible.WorkspaceKey, history.WorkspaceKey);
        Assert.Equal(visible.Id, history.JobPostingId);
        Assert.Equal("NEW", history.PreviousStatus);
        Assert.Equal("APPLIED", history.NewStatus);
    }

    [Fact]
    public async Task Invalid_status_is_rejected_without_mutation()
    {
        await using var db = CreateDbContext();
        var posting = CreatePosting("demo:visible", "NEW");
        db.JobPostings.Add(posting);
        await db.SaveChangesAsync();
        var service = new ApplicationStatusService(
            new ApplicationStatusStore(db),
            new StubDomainConfigurationProvider(),
            TimeProvider.System);

        var outcome = await service.ChangeAsync(
            posting.WorkspaceKey,
            posting.Id,
            "NOT_REAL",
            CancellationToken.None);

        Assert.Equal(ChangeApplicationStatusOutcome.InvalidStatus, outcome);
        Assert.Equal("NEW", posting.ApplicationStatus);
        Assert.Empty(db.ApplicationStatusHistory);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    private static JobPosting CreatePosting(
        string workspace,
        string status) => new()
        {
            WorkspaceKey = workspace,
            ProviderKey = "linkedin",
            ProviderExternalId = Guid.NewGuid().ToString(),
            Title = "Engineer",
            NormalizedDataJson = "{}",
            SearchDataJson = "{}",
            ApplicationStatus = status,
            ParserKey = "linkedin",
            ParserVersion = 1,
            SourceReceivedAt = DateTimeOffset.UtcNow
        };

    private sealed class StubDomainConfigurationProvider : IDomainConfigurationProvider
    {
        public DomainConfiguration Current { get; } = new(
            new("Job", "Jobs"),
            new("CV", "CVs"),
            [new("title", "Title", "string", true)],
            [new("NEW", "New"), new("APPLIED", "Applied")]);

        public bool IsWorkflowStatusAllowed(string status) =>
            Current.Statuses.Any(candidate => candidate.Code == status);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
