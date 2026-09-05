using App.Application;
using App.Domain;
using App.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace App.Tests;

public sealed class CvGenerationServiceTests
{
    [Fact]
    public async Task Persists_job_before_enqueuing_its_identifier()
    {
        await using var db = CreateDbContext();
        const string workspace = "demo:generation";
        var posting = CreatePosting(workspace);
        var template = new CvTemplateVersion
        {
            WorkspaceKey = workspace,
            CvTemplateId = Guid.NewGuid(),
            Version = 1,
            Tex = "template"
        };
        var rules = new CandidateRuleVersion
        {
            WorkspaceKey = workspace,
            CandidateRuleDocumentId = Guid.NewGuid(),
            Version = 1,
            Markdown = "rules"
        };
        db.AddRange(posting, template, rules);
        await db.SaveChangesAsync();
        var queue = new InspectingGenerationQueue(db);
        var service = CreateService(db, queue);

        var result = await service.RequestAsync(
            workspace,
            new RequestCvGeneration(
                posting.Id,
                template.Id,
                rules.Id,
                "  Custom role requirements  ",
                null),
            CancellationToken.None);

        Assert.Equal(RequestCvGenerationOutcome.Accepted, result.Outcome);
        Assert.NotNull(result.Job);
        Assert.Equal(result.Job.Id, queue.EnqueuedJobId);
        Assert.True(queue.JobExistedWhenEnqueued);
        var persisted = await db.CvGenerationJobs.SingleAsync();
        Assert.Equal("rules-hash", persisted.SystemRulesHash);
        Assert.DoesNotContain("demoEmailHtml", persisted.JobPostingSnapshotJson);
        Assert.Contains(
            "\"customJobDescription\":\"Custom role requirements\"",
            persisted.JobPostingSnapshotJson);
    }

    [Fact]
    public async Task Rejects_inputs_owned_by_another_workspace()
    {
        await using var db = CreateDbContext();
        var posting = CreatePosting("demo:other");
        db.JobPostings.Add(posting);
        await db.SaveChangesAsync();
        var queue = new InspectingGenerationQueue(db);
        var service = CreateService(db, queue);

        var result = await service.RequestAsync(
            "demo:current",
            new RequestCvGeneration(posting.Id, Guid.NewGuid(), Guid.NewGuid(), null, null),
            CancellationToken.None);

        Assert.Equal(RequestCvGenerationOutcome.InvalidInput, result.Outcome);
        Assert.Null(queue.EnqueuedJobId);
    }

    [Fact]
    public async Task Enforces_workspace_quota_atomically_before_enqueue()
    {
        await using var db = CreateDbContext();
        const string workspace = "demo:quota";
        var posting = CreatePosting(workspace);
        var template = new CvTemplateVersion
        {
            WorkspaceKey = workspace,
            CvTemplateId = Guid.NewGuid(),
            Version = 1,
            Tex = "template"
        };
        var rules = new CandidateRuleVersion
        {
            WorkspaceKey = workspace,
            CandidateRuleDocumentId = Guid.NewGuid(),
            Version = 1,
            Markdown = "rules"
        };
        db.AddRange(posting, template, rules, new CvGenerationJob
        {
            WorkspaceKey = workspace,
            JobPostingSnapshotJson = "{}",
            SystemRulesHash = "hash",
            Model = "test"
        });
        await db.SaveChangesAsync();
        var queue = new InspectingGenerationQueue(db);
        var service = CreateService(db, queue, maximumJobs: 1);

        var result = await service.RequestAsync(
            workspace,
            new RequestCvGeneration(posting.Id, template.Id, rules.Id, null, null),
            CancellationToken.None);

        Assert.Equal(RequestCvGenerationOutcome.QuotaExceeded, result.Outcome);
        Assert.Null(queue.EnqueuedJobId);
        Assert.Equal(1, await db.CvGenerationJobs.CountAsync());
    }

    [Fact]
    public async Task Rejects_custom_job_description_over_the_configured_limit()
    {
        await using var db = CreateDbContext();
        var queue = new InspectingGenerationQueue(db);
        var service = CreateService(db, queue);

        var result = await service.RequestAsync(
            "demo:limit",
            new RequestCvGeneration(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                new string('x', 20_001),
                null),
            CancellationToken.None);

        Assert.Equal(RequestCvGenerationOutcome.InvalidInput, result.Outcome);
        Assert.Null(queue.EnqueuedJobId);
        Assert.Empty(db.CvGenerationJobs);
    }

    private static CvGenerationService CreateService(
        AppDbContext db,
        ICvGenerationQueue queue,
        int maximumJobs = 10) =>
        new(
            new CvGenerationStore(db),
            queue,
            new StubSystemRulesProvider(),
            new CvWorkflowSettings(maximumJobs, 10, 5_000, 20_000, "test-model"));

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    private static JobPosting CreatePosting(string workspace) => new()
    {
        WorkspaceKey = workspace,
        ProviderKey = "linkedin",
        ProviderExternalId = Guid.NewGuid().ToString(),
        Title = "Senior .NET Engineer",
        NormalizedDataJson = """{"company":"Example","description":"Build .NET services"}""",
        SearchDataJson = "{}",
        ApplicationStatus = "NEW",
        ParserKey = "linkedin",
        ParserVersion = 1,
        SourceReceivedAt = DateTimeOffset.UtcNow,
        DemoEmailHtml = "sensitive raw email"
    };

    private sealed class InspectingGenerationQueue(AppDbContext db) : ICvGenerationQueue
    {
        public Guid? EnqueuedJobId { get; private set; }
        public bool JobExistedWhenEnqueued { get; private set; }

        public async Task EnqueueAsync(Guid cvGenerationJobId, CancellationToken cancellationToken)
        {
            EnqueuedJobId = cvGenerationJobId;
            JobExistedWhenEnqueued = await db.CvGenerationJobs.AnyAsync(
                job => job.Id == cvGenerationJobId,
                cancellationToken);
        }
    }

    private sealed class StubSystemRulesProvider : ISystemRulesProvider
    {
        public string GenerationRulesMarkdown => "system rules";
        public string GenerationRulesHash => "rules-hash";
    }
}
