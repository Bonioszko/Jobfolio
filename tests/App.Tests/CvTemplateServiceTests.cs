using App.Application;
using App.Domain;
using App.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace App.Tests;

public sealed class CvTemplateServiceTests
{
    private const string SafeTex = """
        \documentclass{article}
        \begin{document}
        CV
        \end{document}
        """;

    [Fact]
    public async Task ListAsync_initializes_three_examples_for_a_new_workspace()
    {
        await using var db = CreateDatabase();
        var service = new CvTemplateQueryService(db);

        var result = await service.ListAsync("user:owner", CancellationToken.None);

        Assert.Equal(3, result.Count);
        Assert.Equal(["example-1", "example-2", "example-3"], result.Select(item => item.Name));
        Assert.All(result, item => Assert.Equal(1, item.Version));
        Assert.Equal(3, await db.CvTemplates.CountAsync());
        Assert.Equal(3, await db.CvTemplateVersions.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_saves_a_custom_template_in_the_current_workspace()
    {
        await using var db = CreateDatabase();
        var service = CreateCommandService(db);

        var result = await service.CreateAsync(
            "user:owner",
            new SaveCvTemplate("Backend CV", SafeTex),
            CancellationToken.None);

        Assert.Equal(SaveCvTemplateOutcome.Saved, result.Outcome);
        Assert.Equal("Backend CV", result.Template!.Name);
        Assert.Equal(1, result.Template.Version);
        Assert.Equal("user:owner", (await db.CvTemplates.SingleAsync()).WorkspaceKey);
    }

    [Fact]
    public async Task UpdateAsync_creates_an_immutable_version()
    {
        await using var db = CreateDatabase();
        var template = new CvTemplate
        {
            WorkspaceKey = "user:owner",
            Name = "Original",
            CurrentVersion = 1
        };
        var originalVersion = new CvTemplateVersion
        {
            WorkspaceKey = template.WorkspaceKey,
            CvTemplateId = template.Id,
            Version = 1,
            Tex = SafeTex
        };
        db.AddRange(template, originalVersion);
        await db.SaveChangesAsync();
        var service = CreateCommandService(db);
        var updatedTex = SafeTex.Replace("CV", "Updated CV", StringComparison.Ordinal);

        var result = await service.UpdateAsync(
            template.WorkspaceKey,
            template.Id,
            new SaveCvTemplate("Renamed", updatedTex),
            CancellationToken.None);

        Assert.Equal(SaveCvTemplateOutcome.Saved, result.Outcome);
        Assert.Equal(2, result.Template!.Version);
        Assert.Equal("Renamed", template.Name);
        var versions = await db.CvTemplateVersions.OrderBy(version => version.Version).ToListAsync();
        Assert.Equal(2, versions.Count);
        Assert.Equal(SafeTex, versions[0].Tex);
        Assert.Equal(updatedTex, versions[1].Tex);
    }

    [Fact]
    public async Task UpdateAsync_cannot_access_another_workspaces_template()
    {
        await using var db = CreateDatabase();
        var template = new CvTemplate
        {
            WorkspaceKey = "user:other",
            Name = "Private",
            CurrentVersion = 1
        };
        db.CvTemplates.Add(template);
        await db.SaveChangesAsync();
        var service = CreateCommandService(db);

        var result = await service.UpdateAsync(
            "user:owner",
            template.Id,
            new SaveCvTemplate("Stolen", SafeTex),
            CancellationToken.None);

        Assert.Equal(SaveCvTemplateOutcome.NotFound, result.Outcome);
        Assert.Equal("Private", template.Name);
    }

    [Fact]
    public async Task CreateAsync_rejects_unsafe_tex()
    {
        await using var db = CreateDatabase();
        var service = CreateCommandService(db);

        var result = await service.CreateAsync(
            "user:owner",
            new SaveCvTemplate("Unsafe", "\\input{secret.tex}"),
            CancellationToken.None);

        Assert.Equal(SaveCvTemplateOutcome.InvalidInput, result.Outcome);
        Assert.Empty(db.CvTemplates);
    }

    private static CvTemplateCommandService CreateCommandService(AppDbContext db) =>
        new(db, new TexSafetyValidator(200_000));

    private static AppDbContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
