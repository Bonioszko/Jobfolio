using App.Domain;
using App.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace App.Tests;

public sealed class JobPostingPaginationTests
{
    [Fact]
    public async Task ListAsync_returns_stable_newest_first_pages()
    {
        await using var db = CreateDatabase();
        var receivedAt = DateTimeOffset.Parse("2026-09-06T08:00:00Z");
        db.AddRange(
            CreatePosting("Newest", receivedAt.AddMinutes(1), "00000000-0000-0000-0000-000000000001"),
            CreatePosting("Same time higher ID", receivedAt, "00000000-0000-0000-0000-000000000003"),
            CreatePosting("Same time lower ID", receivedAt, "00000000-0000-0000-0000-000000000002"),
            CreatePosting("Oldest", receivedAt.AddMinutes(-1), "00000000-0000-0000-0000-000000000004"));
        await db.SaveChangesAsync();
        var service = new JobPostingQueryService(db);

        var first = await service.ListAsync(
            "user:owner", null, null, null, 2, CancellationToken.None);
        var second = await service.ListAsync(
            "user:owner", null, null, first.NextCursor, 2, CancellationToken.None);

        Assert.Equal(["Newest", "Same time higher ID"],
            first.Items.Select(item => item.DisplayTitle));
        Assert.NotNull(first.NextCursor);
        Assert.Equal(["Same time lower ID", "Oldest"],
            second.Items.Select(item => item.DisplayTitle));
        Assert.Null(second.NextCursor);
    }

    [Fact]
    public async Task ListAsync_does_not_shift_the_next_page_when_a_new_job_arrives()
    {
        await using var db = CreateDatabase();
        var receivedAt = DateTimeOffset.Parse("2026-09-06T08:00:00Z");
        db.AddRange(
            CreatePosting("First", receivedAt, "00000000-0000-0000-0000-000000000003"),
            CreatePosting("Second", receivedAt.AddMinutes(-1), "00000000-0000-0000-0000-000000000002"),
            CreatePosting("Third", receivedAt.AddMinutes(-2), "00000000-0000-0000-0000-000000000001"));
        await db.SaveChangesAsync();
        var service = new JobPostingQueryService(db);

        var first = await service.ListAsync(
            "user:owner", null, null, null, 2, CancellationToken.None);
        db.Add(CreatePosting(
            "New import",
            receivedAt.AddMinutes(1),
            "00000000-0000-0000-0000-000000000004"));
        await db.SaveChangesAsync();
        var second = await service.ListAsync(
            "user:owner", null, null, first.NextCursor, 2, CancellationToken.None);

        var item = Assert.Single(second.Items);
        Assert.Equal("Third", item.DisplayTitle);
    }

    private static AppDbContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static JobPosting CreatePosting(string title, DateTimeOffset receivedAt, string id) =>
        new()
        {
            Id = Guid.Parse(id),
            WorkspaceKey = "user:owner",
            ProviderKey = "linkedin",
            ProviderExternalId = id,
            Title = title,
            NormalizedDataJson = "{}",
            SearchDataJson = "{}",
            ApplicationStatus = "NEW",
            ParserKey = "linkedin",
            ParserVersion = 1,
            SourceReceivedAt = receivedAt
        };
}
