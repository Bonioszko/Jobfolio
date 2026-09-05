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
}
