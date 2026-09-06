using App.Domain;
using App.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace App.Tests;

public sealed class WorkspaceIsolationTests
{
    [Fact]
    public async Task Job_listing_never_returns_another_demo_sessions_item()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new AppDbContext(options);
        db.JobPostings.Add(CreateItem("demo:a", "Visible"));
        db.JobPostings.Add(CreateItem("demo:b", "Secret"));
        await db.SaveChangesAsync();
        var service = new JobPostingQueryService(db);

        var result = await service.ListAsync(
            "demo:a",
            null,
            null,
            null,
            50,
            CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal("Visible", item.DisplayTitle);
    }

    private static JobPosting CreateItem(string workspace, string title) => new()
    {
        WorkspaceKey = workspace,
        ProviderKey = "linkedin",
        ProviderExternalId = title,
        Title = title,
        NormalizedDataJson = "{}",
        SearchDataJson = "{}",
        ApplicationStatus = "NEW",
        ParserKey = "linkedin",
        ParserVersion = 1,
        SourceReceivedAt = DateTimeOffset.UtcNow
    };
}
