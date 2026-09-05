using System.Text.Json;
using App.Application;
using App.Domain;

namespace App.Infrastructure;

public sealed class DemoWorkspaceSeeder(
    AppDbContext db,
    ISourceParserRegistry parserRegistry,
    TimeProvider timeProvider) : IDemoWorkspaceSeeder
{
    private static readonly DemoEmailFixture[] Fixtures =
    [
        new("linkedin-job-01.html", "jobs-noreply@linkedin.com"),
        new("linkedin-job-02.html", "jobs-noreply@linkedin.com"),
        new("justjoinit-job-01.html", "alerts@justjoin.it"),
        new("justjoinit-job-02.html", "alerts@justjoin.it"),
        new("nofluffjobs-job-01.html", "jobs@nofluffjobs.com"),
        new("nofluffjobs-job-02.html", "jobs@nofluffjobs.com")
    ];

    public async Task SeedAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var workspaceKey = $"demo:{sessionId}";
        await SeedJobPostingsAsync(workspaceKey, cancellationToken);
        await SeedCvTemplatesAsync(workspaceKey, cancellationToken);
        SeedCandidateRules(workspaceKey);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedJobPostingsAsync(
        string workspaceKey,
        CancellationToken cancellationToken)
    {
        var fixtureRoot = RepositoryFileLocator.FindDirectory("demo-fixtures", "emails");
        var receivedAt = timeProvider.GetUtcNow();

        for (var index = 0; index < Fixtures.Length; index++)
        {
            var fixture = Fixtures[index];
            var html = await File.ReadAllTextAsync(
                Path.Combine(fixtureRoot, fixture.FileName),
                cancellationToken);
            var email = new EmailMessage(
                fixture.FileName,
                fixture.Sender,
                "New job matching your alert",
                html,
                receivedAt.AddMinutes(-index * 17));
            var selection = parserRegistry.Select(email);

            if (selection.Match != ParserMatch.Matched || selection.Parser is null)
            {
                throw new InvalidOperationException(
                    $"Demo fixture '{fixture.FileName}' did not match exactly one parser.");
            }

            var result = await selection.Parser.ParseAsync(email, cancellationToken);
            db.JobPostings.Add(new JobPosting
            {
                WorkspaceKey = workspaceKey,
                ProviderKey = result.SourceKey,
                ProviderExternalId = result.SourceExternalId,
                Title = result.DisplayTitle,
                NormalizedDataJson = JsonSerializer.Serialize(result.ParsedData, JsonDefaults.Web),
                SearchDataJson = JsonSerializer.Serialize(result.SearchData, JsonDefaults.Web),
                ApplicationStatus = "NEW",
                ParserKey = selection.Parser.Key,
                ParserVersion = selection.Parser.Version,
                SourceReceivedAt = email.ReceivedAt,
                DemoEmailHtml = html
            });
        }
    }

    private async Task SeedCvTemplatesAsync(
        string workspaceKey,
        CancellationToken cancellationToken)
    {
        var templateRoot = RepositoryFileLocator.FindDirectory("example-templates");

        foreach (var file in Directory.EnumerateFiles(templateRoot, "*.tex").Order())
        {
            var template = new CvTemplate
            {
                WorkspaceKey = workspaceKey,
                Name = Path.GetFileNameWithoutExtension(file),
                CurrentVersion = 1
            };
            db.CvTemplates.Add(template);
            db.CvTemplateVersions.Add(new CvTemplateVersion
            {
                WorkspaceKey = workspaceKey,
                CvTemplateId = template.Id,
                Version = 1,
                Tex = await File.ReadAllTextAsync(file, cancellationToken)
            });
        }
    }

    private void SeedCandidateRules(string workspaceKey)
    {
        var rules = new CandidateRuleDocument { WorkspaceKey = workspaceKey, CurrentVersion = 1 };
        db.CandidateRuleDocuments.Add(rules);
        db.CandidateRuleVersions.Add(new CandidateRuleVersion
        {
            WorkspaceKey = workspaceKey,
            CandidateRuleDocumentId = rules.Id,
            Version = 1,
            Markdown = """
                # Candidate facts
                - Name: Alex Morgan
                - 8 years of software engineering experience
                - Skills: C#, .NET, ASP.NET Core, PostgreSQL, SQL, React, TypeScript, Docker, Terraform, Google Cloud, automated testing, CI/CD, distributed systems, mentoring
                - Location: Warsaw, Poland

                Use only these facts as candidate claims. Tailor emphasis to the selected job; never invent experience.
                """
        });
    }

    private sealed record DemoEmailFixture(string FileName, string Sender);
}
