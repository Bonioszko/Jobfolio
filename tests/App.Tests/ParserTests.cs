using App.Application;
using App.Parsers;

namespace App.Tests;

public sealed class ParserTests
{
    [Fact]
    public async Task SourceA_parses_and_normalizes_polish_price()
    {
        var parser = new SourceAParser();
        var email = new EmailMessage("1", "offers@source-a.example", "test", "<p>Title: Phone | Producer: ACME | Price: 1 599,99 zł</p>", DateTimeOffset.UtcNow);
        var result = await parser.ParseAsync(email, CancellationToken.None);
        Assert.Equal("Phone", result.DisplayTitle);
        Assert.Equal("ACME", result.ParsedData["producer"]);
        Assert.Equal(1599.99m, result.SearchData["price"]);
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
    public async Task Missing_required_price_is_rejected()
    {
        var parser = new SourceBParser();
        var email = new EmailMessage("1", "sales@source-b.example", "test", "Title: Phone | Producer: ACME", DateTimeOffset.UtcNow);
        await Assert.ThrowsAsync<FormatException>(() => parser.ParseAsync(email, CancellationToken.None));
    }

    private sealed class AlwaysParser(string key) : ISourceParser
    {
        public string Key => key;
        public int Version => 1;
        public bool CanParse(EmailMessage email) => true;
        public Task<ParseResult> ParseAsync(EmailMessage email, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
