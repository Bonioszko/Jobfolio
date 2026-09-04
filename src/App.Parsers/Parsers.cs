using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using App.Application;

namespace App.Parsers;

public sealed class SourceParserRegistry(IEnumerable<ISourceParser> parsers) : ISourceParserRegistry
{
    private readonly ISourceParser[] _parsers = parsers.ToArray();
    public ParserSelection Select(EmailMessage email)
    {
        var matches = _parsers.Where(p => p.CanParse(email)).ToArray();
        return matches.Length switch
        {
            0 => new(ParserMatch.Unsupported, null),
            1 => new(ParserMatch.Matched, matches[0]),
            _ => new(ParserMatch.Ambiguous, null)
        };
    }
}

public abstract partial class ListingEmailParser : ISourceParser
{
    public abstract string Key { get; }
    public virtual int Version => 1;
    protected abstract string SenderMarker { get; }
    public bool CanParse(EmailMessage email) => email.Sender.Contains(SenderMarker, StringComparison.OrdinalIgnoreCase);

    public Task<ParseResult> ParseAsync(EmailMessage email, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var text = WebUtility.HtmlDecode(TagRegex().Replace(email.HtmlBody, " "));
        var producer = Capture(text, @"Producer\s*:\s*([^|\r\n]+)");
        var priceText = Capture(text, @"Price\s*:\s*([0-9\s.,]+)\s*(PLN|zł)");
        var title = Capture(text, @"Title\s*:\s*([^|\r\n]+)");
        var price = ParsePrice(priceText);
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(producer) || price is null)
            throw new FormatException("Required listing fields were not found.");
        var parsed = new Dictionary<string, object?>
        {
            ["producer"] = producer,
            ["price"] = new Dictionary<string, object?> { ["amount"] = price, ["currency"] = "PLN" }
        };
        var search = new Dictionary<string, object?> { ["producer"] = producer, ["price"] = price };
        return Task.FromResult(new ParseResult(Key, title, parsed, search));
    }

    private static string Capture(string value, string pattern) =>
        Regex.Match(value, pattern, RegexOptions.IgnoreCase).Groups[1].Value.Trim();

    public static decimal? ParsePrice(string value)
    {
        var normalized = Regex.Replace(value, @"\s", "").Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : null;
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex TagRegex();
}

public sealed class SourceAParser : ListingEmailParser { public override string Key => "source-a"; protected override string SenderMarker => "source-a.example"; }
public sealed class SourceBParser : ListingEmailParser { public override string Key => "source-b"; protected override string SenderMarker => "source-b.example"; }
public sealed class SourceCParser : ListingEmailParser { public override string Key => "source-c"; protected override string SenderMarker => "source-c.example"; }
