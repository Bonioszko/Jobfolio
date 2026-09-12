using App.Application;
using App.Domain;
using App.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace App.Tests;

public sealed class CvCompilationJobProcessorTests
{
    [Fact]
    public async Task Id_addressed_processing_compiles_only_requested_job()
    {
        await using var db = CreateDbContext();
        var first = AddTemplateJob(db, "demo:first");
        var requested = AddTemplateJob(db, "demo:requested");
        await db.SaveChangesAsync();
        var processor = CreateProcessor(db);

        var outcome = await processor.ProcessAsync(requested.Id, CancellationToken.None);

        Assert.Equal(CvCompilationProcessingOutcome.Processed, outcome);
        Assert.Equal(JobStatus.Queued, first.Status);
        Assert.Equal(JobStatus.Succeeded, requested.Status);
        Assert.Equal(1, requested.AttemptCount);
        Assert.NotNull(requested.PdfArtifactId);
    }

    [Fact]
    public async Task Active_delivery_is_deferred_but_terminal_duplicate_is_acknowledged()
    {
        await using var db = CreateDbContext();
        var active = AddTemplateJob(db, "demo:active");
        active.MarkClaimed(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(5));
        var terminal = AddTemplateJob(db, "demo:terminal");
        terminal.MarkClaimed(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(5));
        terminal.MarkFailed(DateTimeOffset.UtcNow, "invalid template");
        await db.SaveChangesAsync();
        var processor = CreateProcessor(db);

        var activeOutcome = await processor.ProcessAsync(active.Id, CancellationToken.None);
        var terminalOutcome = await processor.ProcessAsync(terminal.Id, CancellationToken.None);

        Assert.Equal(CvCompilationProcessingOutcome.Deferred, activeOutcome);
        Assert.Equal(CvCompilationProcessingOutcome.AlreadyTerminal, terminalOutcome);
    }

    private static CvCompileJob AddTemplateJob(AppDbContext db, string workspace)
    {
        var version = new CvTemplateVersion
        {
            WorkspaceKey = workspace,
            CvTemplateId = Guid.NewGuid(),
            Version = 1,
            Tex = "\\documentclass{article}"
        };
        var job = new CvCompileJob
        {
            WorkspaceKey = workspace,
            CvTemplateVersionId = version.Id
        };
        db.CvTemplateVersions.Add(version);
        db.CvCompileJobs.Add(job);
        return job;
    }

    private static CvCompilationJobProcessor CreateProcessor(AppDbContext db) =>
        new(
            new CvCompilationStore(db),
            new TestCompiler(),
            new TestStorage(),
            TimeProvider.System);

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    private sealed class TestCompiler : ITexCompiler
    {
        public Task<byte[]> CompileAsync(string tex, CancellationToken cancellationToken) =>
            Task.FromResult("pdf"u8.ToArray());
    }

    private sealed class TestStorage : IArtifactStorage
    {
        public Task<StoredArtifact> SaveAsync(
            string workspaceKey,
            Guid artifactId,
            byte[] data,
            CancellationToken cancellationToken) =>
            Task.FromResult(new StoredArtifact(
                artifactId,
                $"{artifactId:N}.pdf",
                "hash",
                data.Length));

        public Task<Stream?> OpenReadAsync(
            string key,
            CancellationToken cancellationToken) => Task.FromResult<Stream?>(null);

        public Task DeleteWorkspaceAsync(
            string workspaceKey,
            CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
