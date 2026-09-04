using App.Application;
using App.Parsers;
using App.Parsers.Sources;

namespace App.Tests;

public sealed class ParserTests
{
    [Fact]
    public async Task LinkedIn_parser_normalizes_a_job_post()
    {
        var parser = new LinkedInJobParser();
        var email = new EmailMessage("message-1", "jobs@linkedin.com", "test", "<p>Job ID: li-42 | Title: Senior .NET Engineer | Company: Contoso | Location: Warsaw, Poland | Employment Type: Full-time | Salary: 20 000-25 000 PLN | Description: Build distributed .NET services | URL: https://example.test/jobs/li-42</p>", DateTimeOffset.UtcNow);
        var result = await parser.ParseAsync(email, CancellationToken.None);
        Assert.Equal("Senior .NET Engineer", result.DisplayTitle);
        Assert.Equal("Contoso", result.ParsedData["company"]);
        Assert.Equal("li-42", result.SourceExternalId);
        Assert.Equal("Warsaw, Poland", result.SearchData["location"]);
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
    public async Task Missing_required_description_is_rejected()
    {
        var parser = new JustJoinItJobParser();
        var email = new EmailMessage("1", "alerts@justjoin.it", "test", "Job ID: jj-1 | Title: Engineer | Company: ACME", DateTimeOffset.UtcNow);
        await Assert.ThrowsAsync<FormatException>(() => parser.ParseAsync(email, CancellationToken.None));
    }

    [Theory]
    [InlineData("linkedin-job-01.html", "jobs@linkedin.com")]
    [InlineData("linkedin-job-02.html", "jobs@linkedin.com")]
    [InlineData("justjoinit-job-01.html", "alerts@justjoin.it")]
    [InlineData("justjoinit-job-02.html", "alerts@justjoin.it")]
    [InlineData("nofluffjobs-job-01.html", "jobs@nofluffjobs.com")]
    [InlineData("nofluffjobs-job-02.html", "jobs@nofluffjobs.com")]
    public async Task Demo_fixture_matches_exactly_one_parser_and_parses(string file, string sender)
    {
        var registry = new SourceParserRegistry([new LinkedInJobParser(), new JustJoinItJobParser(), new NoFluffJobsParser()]);
        var root = FindRepositoryDirectory();
        var html = await File.ReadAllTextAsync(Path.Combine(root, "demo-fixtures", "emails", file));
        var selection = registry.Select(new EmailMessage(file, sender, "Job alert", html, DateTimeOffset.UtcNow));

        Assert.Equal(ParserMatch.Matched, selection.Match);
        var result = await selection.Parser!.ParseAsync(new EmailMessage(file, sender, "Job alert", html, DateTimeOffset.UtcNow), CancellationToken.None);
        Assert.False(string.IsNullOrWhiteSpace(result.SourceExternalId));
        Assert.False(string.IsNullOrWhiteSpace(result.DisplayTitle));
        Assert.True(result.ParsedData.ContainsKey("description"));
    }

    private static string FindRepositoryDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "demo-fixtures"))) directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }

    private sealed class AlwaysParser(string key) : ISourceParser
    {
        public string Key => key;
        public int Version => 1;
        public bool CanParse(EmailMessage email) => true;
        public Task<ParseResult> ParseAsync(EmailMessage email, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
