using System.Text.Json;
using App.Application;
using App.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace App.Tests;

public sealed class ManualJobPostingServiceTests
{
    [Fact]
    public async Task CreateAsync_ValidInput_PersistsNormalizedWorkspaceOwnedManualPosting()
    {
        await using var db = CreateDbContext();
        var now = new DateTimeOffset(2026, 9, 14, 12, 30, 0, TimeSpan.Zero);
        var service = new ManualJobPostingService(db, new FixedTimeProvider(now));

        var result = await service.CreateAsync(
            "demo:owner",
            new CreateManualJobPosting(
                "  Senior .NET Developer  ",
                "  Example Company  ",
                "  Warsaw / Remote  ",
                "  B2B  ",
                "  20 000-26 000 PLN  ",
                "  Build and maintain the platform.  ",
                "  https://example.com/jobs/backend?id=42  "),
            CancellationToken.None);

        Assert.Equal(CreateManualJobPostingOutcome.Created, result.Outcome);
        Assert.Null(result.ValidationErrors);
        var view = Assert.IsType<JobPostingView>(result.Posting);
        Assert.Equal("manual", view.SourceKey);
        Assert.Equal("Senior .NET Developer", view.DisplayTitle);
        Assert.Equal("NEW", view.WorkflowStatus);
        Assert.Equal("manual", view.ParserKey);
        Assert.Equal(1, view.ParserVersion);
        Assert.Equal(now, view.SourceReceivedAt);

        var posting = await db.JobPostings.SingleAsync();
        Assert.Equal("demo:owner", posting.WorkspaceKey);
        Assert.Equal("manual", posting.ProviderKey);
        Assert.Null(posting.ProviderExternalId);
        Assert.Equal("Senior .NET Developer", posting.Title);
        Assert.Equal("NEW", posting.ApplicationStatus);
        Assert.Equal("manual", posting.ParserKey);
        Assert.Equal(1, posting.ParserVersion);
        Assert.Equal(now, posting.SourceReceivedAt);
        Assert.Equal(now, posting.CreatedAt);
        Assert.Equal(now, posting.UpdatedAt);

        using var parsedData = JsonDocument.Parse(posting.NormalizedDataJson);
        Assert.Equal("Example Company", parsedData.RootElement.GetProperty("company").GetString());
        Assert.Equal("Warsaw / Remote", parsedData.RootElement.GetProperty("location").GetString());
        Assert.Equal("B2B", parsedData.RootElement.GetProperty("employmentType").GetString());
        Assert.Equal("20 000-26 000 PLN", parsedData.RootElement.GetProperty("salary").GetString());
        Assert.Equal(
            "Build and maintain the platform.",
            parsedData.RootElement.GetProperty("description").GetString());
        Assert.Equal(
            "https://example.com/jobs/backend?id=42",
            parsedData.RootElement.GetProperty("url").GetString());
    }

    [Theory]
    [InlineData("", "Company", "Description", "https://example.com/job", "title")]
    [InlineData("Engineer", "", "Description", "https://example.com/job", "company")]
    [InlineData("Engineer", "Company", "", "https://example.com/job", "description")]
    [InlineData("Engineer", "Company", "Description", "javascript:alert(1)", "url")]
    [InlineData("Engineer", "Company", "Description", "https://user:secret@example.com/job", "url")]
    public async Task CreateAsync_InvalidInput_DoesNotPersist(
        string title,
        string company,
        string description,
        string url,
        string invalidField)
    {
        await using var db = CreateDbContext();
        var service = new ManualJobPostingService(db, TimeProvider.System);

        var result = await service.CreateAsync(
            "demo:owner",
            new CreateManualJobPosting(title, company, null, null, null, description, url),
            CancellationToken.None);

        Assert.Equal(CreateManualJobPostingOutcome.InvalidInput, result.Outcome);
        Assert.Null(result.Posting);
        var validationErrors = Assert.IsAssignableFrom<IReadOnlyDictionary<string, string[]>>(
            result.ValidationErrors);
        Assert.Contains(invalidField, validationErrors.Keys);
        Assert.Empty(db.JobPostings);
    }

    [Fact]
    public async Task CreateAsync_BlankOptionalValues_AreStoredAsNull()
    {
        await using var db = CreateDbContext();
        var service = new ManualJobPostingService(db, TimeProvider.System);

        var result = await service.CreateAsync(
            "user:owner",
            new CreateManualJobPosting(
                "Engineer",
                "Example Company",
                "  ",
                string.Empty,
                null,
                "Description",
                "  "),
            CancellationToken.None);

        Assert.Equal(CreateManualJobPostingOutcome.Created, result.Outcome);
        var posting = await db.JobPostings.SingleAsync();
        using var parsedData = JsonDocument.Parse(posting.NormalizedDataJson);
        Assert.Equal(JsonValueKind.Null, parsedData.RootElement.GetProperty("location").ValueKind);
        Assert.Equal(JsonValueKind.Null, parsedData.RootElement.GetProperty("employmentType").ValueKind);
        Assert.Equal(JsonValueKind.Null, parsedData.RootElement.GetProperty("salary").ValueKind);
        Assert.Equal(JsonValueKind.Null, parsedData.RootElement.GetProperty("url").ValueKind);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
