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
        db.SourceItems.Add(CreateItem("demo:a", "Visible"));
        db.SourceItems.Add(CreateItem("demo:b", "Secret"));
        await db.SaveChangesAsync();
        var service = new WorkspaceApplicationService(db);

        var result = await service.GetSourceItemsAsync("demo:a", null, null, 50, CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal("Visible", item.DisplayTitle);
    }

    [Theory]
    [InlineData(@"\write18{calc}")]
    [InlineData(@"\input{../secret}")]
    [InlineData(@"C:\private\file")]
    public void Unsafe_tex_is_rejected(string tex) => Assert.Throws<InvalidOperationException>(() => TexSafety.Validate(tex));

    private static SourceItem CreateItem(string workspace, string title) => new()
    {
        WorkspaceKey = workspace, SourceKey = "linkedin", SourceExternalId = title, DisplayTitle = title,
        ParsedDataJson = "{}", SearchDataJson = "{}", WorkflowStatus = "NEW", ParserKey = "linkedin", ParserVersion = 1,
        SourceReceivedAt = DateTimeOffset.UtcNow
    };
}
