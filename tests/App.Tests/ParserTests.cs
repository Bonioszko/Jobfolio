using App.Application;
using App.Parsers;
using App.Parsers.Sources;

namespace App.Tests;

public sealed class ParserTests
{
    [Fact]
    public async Task NoFluffJobs_parser_imports_every_card_and_unwraps_tracking_url()
    {
        var email = await FixtureEmailAsync(
            "nofluffjobs-alert.html",
            "No Fluff Jobs <notifications@nofluffjobs.com>");

        var results = await new NoFluffJobsParser().ParseAsync(email, CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.Collection(
            results,
            first =>
            {
                Assert.Equal("platform-engineer-aurora-labs", first.SourceExternalId);
                Assert.Equal("Platform Engineer", first.DisplayTitle);
                Assert.Equal("Aurora Labs", first.ParsedData["company"]);
                Assert.Null(first.ParsedData["description"]);
                Assert.Equal(
                    "https://nofluffjobs.com/pl/job/platform-engineer-aurora-labs",
                    first.ParsedData["url"]);
            },
            second => Assert.Equal("junior-dotnet-engineer-northstar", second.SourceExternalId));
    }

    [Fact]
    public async Task JustJoinIt_parser_imports_every_offer()
    {
        var email = await FixtureEmailAsync("justjoinit-alert.html", "no-reply@justjoin.it");

        var results = await new JustJoinItJobParser().ParseAsync(email, CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.Equal("senior-backend-engineer-vela-systems", results[0].SourceExternalId);
        Assert.Equal("Vela Systems", results[0].SearchData["company"]);
        Assert.Equal("B2B", results[0].SearchData["employmentType"]);
        Assert.Equal(
            "https://justjoin.it/job-offer/senior-backend-engineer-vela-systems",
            results[0].ParsedData["url"]);
    }

    [Fact]
    public async Task LinkedIn_parser_imports_every_plain_text_result()
    {
        var email = await FixtureEmailAsync(
            "linkedin-alert.txt",
            "LinkedIn Job Alerts <jobalerts-noreply@linkedin.com>",
            isText: true);

        var results = await new LinkedInJobParser().ParseAsync(email, CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.Equal("4100000001", results[0].SourceExternalId);
        Assert.Equal("Backend Engineer", results[0].DisplayTitle);
        Assert.Equal("Warsaw, Mazowieckie, Poland", results[0].SearchData["location"]);
        Assert.Equal("https://www.linkedin.com/jobs/view/4100000001", results[0].ParsedData["url"]);
    }

    [Fact]
    public async Task Indeed_parser_imports_available_description_snippet()
    {
        var email = await FixtureEmailAsync(
            "indeed-alert.txt",
            "Indeed <donotreply@jobalert.indeed.com>",
            isText: true);

        var results = await new IndeedJobParser().ParseAsync(email, CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.Equal("abc123def456", results[0].SourceExternalId);
        Assert.Equal("Stonebridge", results[0].ParsedData["company"]);
        Assert.Equal("Remote", results[0].ParsedData["location"]);
        Assert.Equal(
            "Build and maintain APIs for a logistics platform.",
            results[0].ParsedData["description"]);
        Assert.Equal("https://pl.indeed.com/viewjob?jk=abc123def456", results[0].ParsedData["url"]);
    }

    [Fact]
    public async Task PracujPl_parser_imports_every_offer_from_real_alert_layout()
    {
        var email = await FixtureEmailAsync(
            "pracujpl-alert.html",
            "Wyszukiwane w Pracuj.pl <jobalert@wysylka.pracuj.pl>");
        var parser = new PracujPlJobParser();

        Assert.True(parser.CanParse(email));

        var results = await parser.ParseAsync(email, CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.Equal(1, parser.Version);
        Assert.Collection(
            results,
            first =>
            {
                Assert.Equal("1000000001", first.SourceExternalId);
                Assert.Equal("Unity Developer", first.DisplayTitle);
                Assert.Equal("Example Games", first.ParsedData["company"]);
                Assert.Equal("Poznań", first.ParsedData["location"]);
                Assert.Null(first.ParsedData["salary"]);
                Assert.Equal(
                    "https://www.pracuj.pl/praca/unity-developer-poznan,oferta,1000000001",
                    first.ParsedData["url"]);
            },
            second =>
            {
                Assert.Equal("1000000002", second.SourceExternalId);
                Assert.Equal(".NET Software Developer", second.DisplayTitle);
                Assert.Equal("Example Systems Sp. z o.o.", second.SearchData["company"]);
                Assert.Equal("Warszawa, Wola", second.SearchData["location"]);
                Assert.Equal("14 000-23 000 zł netto (+ VAT) / mies.", second.ParsedData["salary"]);
            });
    }

    [Theory]
    [InlineData("alerts@nofluffjobs.com.attacker.test")]
    [InlineData("not-linkedin@example.test")]
    [InlineData("invalid sender")]
    public void Parser_rejects_sender_domain_spoofing(string sender)
    {
        Assert.False(new NoFluffJobsParser().CanParse(
            new EmailMessage("1", sender, "subject", "body", DateTimeOffset.UtcNow)));
    }

    [Fact]
    public void Registry_does_not_guess_when_multiple_parsers_match()
    {
        var registry = new SourceParserRegistry([new AlwaysParser("one"), new AlwaysParser("two")]);
        var result = registry.Select(new EmailMessage("1", "sender", "subject", "body", DateTimeOffset.UtcNow));
        Assert.Equal(ParserMatch.Ambiguous, result.Match);
        Assert.Null(result.Parser);
    }

    [Fact]
    public async Task Recognized_sender_with_unrecognized_content_fails_explicitly()
    {
        var parser = new JustJoinItJobParser();
        var email = new EmailMessage(
            "1",
            "alerts@justjoin.it",
            "test",
            "<p>This layout has no offers.</p>",
            DateTimeOffset.UtcNow);

        await Assert.ThrowsAsync<FormatException>(() => parser.ParseAsync(email, CancellationToken.None));
    }

    [Theory]
    [InlineData("linkedin-job-01.html", "jobs@linkedin.com")]
    [InlineData("linkedin-job-02.html", "jobs@linkedin.com")]
    [InlineData("justjoinit-job-01.html", "alerts@justjoin.it")]
    [InlineData("justjoinit-job-02.html", "alerts@justjoin.it")]
    [InlineData("nofluffjobs-job-01.html", "jobs@nofluffjobs.com")]
    [InlineData("nofluffjobs-job-02.html", "jobs@nofluffjobs.com")]
    public async Task Demo_fixture_matches_the_provider_format(string file, string sender)
    {
        var registry = new SourceParserRegistry(
            [
                new LinkedInJobParser(),
                new JustJoinItJobParser(),
                new NoFluffJobsParser(),
                new IndeedJobParser(),
                new PracujPlJobParser()
            ]);
        var email = await FixtureEmailAsync(
            Path.Combine("demo-fixtures", "emails", file),
            sender,
            useRepositoryRelativePath: true);

        var selection = registry.Select(email);

        Assert.Equal(ParserMatch.Matched, selection.Match);
        var result = Assert.Single(await selection.Parser!.ParseAsync(email, CancellationToken.None));
        Assert.False(string.IsNullOrWhiteSpace(result.SourceExternalId));
        Assert.False(string.IsNullOrWhiteSpace(result.DisplayTitle));
        Assert.True(result.ParsedData.ContainsKey("description"));
    }

    private static async Task<EmailMessage> FixtureEmailAsync(
        string file,
        string sender,
        bool isText = false,
        bool useRepositoryRelativePath = false)
    {
        var root = FindRepositoryDirectory();
        var path = useRepositoryRelativePath
            ? Path.Combine(root, file)
            : Path.Combine(root, "tests", "App.Tests", "Fixtures", "EmailAlerts", file);
        var content = await File.ReadAllTextAsync(path);
        return new EmailMessage(
            Path.GetFileName(file),
            sender,
            "Job alert",
            isText ? string.Empty : content,
            DateTimeOffset.UtcNow,
            isText ? content : null);
    }

    private static string FindRepositoryDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "demo-fixtures")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }

    private sealed class AlwaysParser(string key) : ISourceParser
    {
        public string Key => key;
        public int Version => 1;
        public bool CanParse(EmailMessage email) => true;
        public Task<IReadOnlyList<ParseResult>> ParseAsync(
            EmailMessage email,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
