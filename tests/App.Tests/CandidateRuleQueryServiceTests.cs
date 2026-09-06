using App.Domain;
using App.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace App.Tests;

public sealed class CandidateRuleQueryServiceTests
{
    [Fact]
    public async Task GetCurrentAsync_initializes_rules_for_a_new_workspace()
    {
        await using var db = CreateDatabase();
        var service = new CandidateRuleQueryService(db);

        var result = await service.GetCurrentAsync("user:owner", CancellationToken.None);

        Assert.Equal(1, result.Version);
        Assert.Contains("No verified candidate facts", result.Markdown);
        var document = await db.CandidateRuleDocuments.SingleAsync();
        var version = await db.CandidateRuleVersions.SingleAsync();
        Assert.Equal("user:owner", document.WorkspaceKey);
        Assert.Equal(document.Id, version.CandidateRuleDocumentId);
        Assert.Equal(result.VersionId, version.Id);
    }

    [Fact]
    public async Task GetCurrentAsync_reuses_the_existing_version()
    {
        await using var db = CreateDatabase();
        var document = new CandidateRuleDocument
        {
            WorkspaceKey = "user:owner",
            CurrentVersion = 2
        };
        var version = new CandidateRuleVersion
        {
            WorkspaceKey = document.WorkspaceKey,
            CandidateRuleDocumentId = document.Id,
            Version = 2,
            Markdown = "# Verified facts"
        };
        db.AddRange(document, version);
        await db.SaveChangesAsync();
        var service = new CandidateRuleQueryService(db);

        var result = await service.GetCurrentAsync(document.WorkspaceKey, CancellationToken.None);

        Assert.Equal(document.Id, result.Id);
        Assert.Equal(version.Id, result.VersionId);
        Assert.Equal("# Verified facts", result.Markdown);
        Assert.Equal(1, await db.CandidateRuleDocuments.CountAsync());
        Assert.Equal(1, await db.CandidateRuleVersions.CountAsync());
    }

    [Fact]
    public async Task GetCurrentAsync_does_not_return_another_workspaces_rules()
    {
        await using var db = CreateDatabase();
        var otherDocument = new CandidateRuleDocument
        {
            WorkspaceKey = "user:other",
            CurrentVersion = 1
        };
        db.Add(otherDocument);
        db.Add(new CandidateRuleVersion
        {
            WorkspaceKey = otherDocument.WorkspaceKey,
            CandidateRuleDocumentId = otherDocument.Id,
            Version = 1,
            Markdown = "# Other user's private facts"
        });
        await db.SaveChangesAsync();
        var service = new CandidateRuleQueryService(db);

        var result = await service.GetCurrentAsync("user:owner", CancellationToken.None);

        Assert.DoesNotContain("private facts", result.Markdown);
        Assert.Equal(2, await db.CandidateRuleDocuments.CountAsync());
        Assert.Equal(2, await db.CandidateRuleVersions.CountAsync());
    }

    [Fact]
    public async Task GetCurrentAsync_reports_an_inconsistent_current_version()
    {
        await using var db = CreateDatabase();
        db.Add(new CandidateRuleDocument
        {
            WorkspaceKey = "user:owner",
            CurrentVersion = 1
        });
        await db.SaveChangesAsync();
        var service = new CandidateRuleQueryService(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetCurrentAsync("user:owner", CancellationToken.None));

        Assert.Contains("do not reference an existing current version", exception.Message);
        Assert.Equal(1, await db.CandidateRuleDocuments.CountAsync());
        Assert.Empty(db.CandidateRuleVersions);
    }

    private static AppDbContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
