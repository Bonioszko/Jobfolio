using App.Application;
using App.Domain;
using App.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace App.Tests;

public sealed class GmailImportStoreTests
{
    [Fact]
    public async Task Store_is_idempotent_and_scopes_receipts_by_workspace()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);
        var store = new GmailImportStore(db);
        var email = new EmailMessage(
            "gmail-id",
            "jobs@linkedin.com",
            "Jobs",
            "body that must not be persisted",
            DateTimeOffset.UtcNow);
        var posting = new ParseResult(
            "linkedin",
            "123",
            "Developer",
            new() { ["description"] = "Description", ["url"] = "https://www.linkedin.com/jobs/view/123" },
            new());

        await store.SaveAsync("user:a", email, "IMPORTED", "linkedin", 2, [posting], CancellationToken.None);
        await store.SaveAsync("user:a", email, "IMPORTED", "linkedin", 2, [posting], CancellationToken.None);

        Assert.Single(db.GmailMessageReceipts);
        Assert.Single(db.JobPostings);
        Assert.True(await store.IsProcessedAsync("user:a", "gmail-id", CancellationToken.None));
        Assert.False(await store.IsProcessedAsync("user:b", "gmail-id", CancellationToken.None));
        var receiptEntity = db.Model.FindEntityType(typeof(GmailMessageReceipt));
        Assert.NotNull(receiptEntity);
        Assert.DoesNotContain(receiptEntity.GetProperties(), property =>
            property.Name.Contains("Body", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Cross_provider_duplicate_is_stored_once_while_both_emails_are_recorded()
    {
        await using var db = CreateDatabase();
        var store = new GmailImportStore(db);
        var receivedAt = new DateTimeOffset(2026, 9, 13, 8, 0, 0, TimeSpan.Zero);

        await store.SaveAsync(
            "user:owner",
            CreateEmail("linkedin-email", receivedAt),
            "IMPORTED",
            "linkedin",
            2,
            [CreatePosting(
                "linkedin",
                "linkedin-123",
                "Senior .NET Engineer",
                "Acme S.A.",
                "Warsaw, Poland")],
            CancellationToken.None);
        await store.SaveAsync(
            "user:owner",
            CreateEmail("indeed-email", receivedAt.AddMinutes(1)),
            "IMPORTED",
            "indeed",
            2,
            [CreatePosting(
                "indeed",
                "indeed-456",
                " senior .net engineer ",
                "ACME S.A.",
                "Warsaw / Poland")],
            CancellationToken.None);

        var posting = await db.JobPostings.SingleAsync();
        Assert.Equal("linkedin", posting.ProviderKey);
        Assert.Equal("linkedin-123", posting.ProviderExternalId);
        Assert.Equal(2, await db.GmailMessageReceipts.CountAsync());
        Assert.Contains(
            await db.GmailMessageReceipts.Select(receipt => receipt.GmailMessageId).ToListAsync(),
            id => id == "indeed-email");
    }

    [Fact]
    public async Task Cross_provider_deduplication_is_scoped_to_workspace()
    {
        await using var db = CreateDatabase();
        var store = new GmailImportStore(db);
        var receivedAt = new DateTimeOffset(2026, 9, 13, 8, 0, 0, TimeSpan.Zero);
        var posting = CreatePosting(
            "linkedin",
            "123",
            "Platform Engineer",
            "Northstar",
            "Remote");

        await store.SaveAsync(
            "user:first",
            CreateEmail("first-email", receivedAt),
            "IMPORTED",
            "linkedin",
            2,
            [posting],
            CancellationToken.None);
        await store.SaveAsync(
            "user:second",
            CreateEmail("second-email", receivedAt),
            "IMPORTED",
            "indeed",
            2,
            [posting with { SourceKey = "indeed", SourceExternalId = "456" }],
            CancellationToken.None);

        Assert.Equal(2, await db.JobPostings.CountAsync());
        Assert.Equal(
            ["user:first", "user:second"],
            await db.JobPostings
                .OrderBy(item => item.WorkspaceKey)
                .Select(item => item.WorkspaceKey)
                .ToArrayAsync());
    }

    [Fact]
    public async Task Same_role_in_different_known_locations_is_not_deduplicated()
    {
        await using var db = CreateDatabase();
        var store = new GmailImportStore(db);
        var receivedAt = new DateTimeOffset(2026, 9, 13, 8, 0, 0, TimeSpan.Zero);

        await store.SaveAsync(
            "user:owner",
            CreateEmail("warsaw-email", receivedAt),
            "IMPORTED",
            "linkedin",
            2,
            [CreatePosting("linkedin", "123", "Developer", "Acme", "Warsaw")],
            CancellationToken.None);
        await store.SaveAsync(
            "user:owner",
            CreateEmail("krakow-email", receivedAt.AddMinutes(1)),
            "IMPORTED",
            "indeed",
            2,
            [CreatePosting("indeed", "456", "Developer", "Acme", "Kraków")],
            CancellationToken.None);

        Assert.Equal(2, await db.JobPostings.CountAsync());
        Assert.Equal(2, await db.GmailMessageReceipts.CountAsync());
    }

    [Fact]
    public async Task Same_provider_identity_in_a_new_email_is_stored_once()
    {
        await using var db = CreateDatabase();
        var store = new GmailImportStore(db);
        var receivedAt = new DateTimeOffset(2026, 9, 13, 8, 0, 0, TimeSpan.Zero);

        await store.SaveAsync(
            "user:owner",
            CreateEmail("first-email", receivedAt),
            "IMPORTED",
            "linkedin",
            2,
            [CreatePosting("linkedin", "123", "Developer", "Acme", "Warsaw")],
            CancellationToken.None);
        await store.SaveAsync(
            "user:owner",
            CreateEmail("second-email", receivedAt.AddMinutes(1)),
            "IMPORTED",
            "linkedin",
            2,
            [CreatePosting("linkedin", "123", "Renamed role", "Contoso", "Remote")],
            CancellationToken.None);

        var posting = await db.JobPostings.SingleAsync();
        Assert.Equal("Developer", posting.Title);
        Assert.Equal(2, await db.GmailMessageReceipts.CountAsync());
    }

    [Fact]
    public async Task Content_duplicates_in_the_same_email_are_stored_once()
    {
        await using var db = CreateDatabase();
        var store = new GmailImportStore(db);
        var receivedAt = new DateTimeOffset(2026, 9, 13, 8, 0, 0, TimeSpan.Zero);

        await store.SaveAsync(
            "user:owner",
            CreateEmail("aggregated-email", receivedAt),
            "IMPORTED",
            "aggregator",
            1,
            [
                CreatePosting("aggregator", "123", "Developer", "Acme", "Remote"),
                CreatePosting("aggregator", "456", " developer ", "ACME", null)
            ],
            CancellationToken.None);

        var posting = await db.JobPostings.SingleAsync();
        Assert.Equal("123", posting.ProviderExternalId);
        Assert.Single(db.GmailMessageReceipts);
    }

    private static AppDbContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static EmailMessage CreateEmail(string id, DateTimeOffset receivedAt) => new(
        id,
        "jobs@example.com",
        "Jobs",
        "body that must not be persisted",
        receivedAt);

    private static ParseResult CreatePosting(
        string sourceKey,
        string sourceExternalId,
        string title,
        string company,
        string? location) => new(
        sourceKey,
        sourceExternalId,
        title,
        new()
        {
            ["company"] = company,
            ["location"] = location,
            ["url"] = $"https://example.com/jobs/{sourceExternalId}"
        },
        new());
}
