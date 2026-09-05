using System.Text.RegularExpressions;
using App.Application;

namespace App.Parsers.Sources;

public sealed partial class IndeedJobParser() : JobEmailParserBase("indeed.com")
{
    public override string Key => "indeed";

    protected override IReadOnlyList<ParseResult> Parse(EmailMessage email)
    {
        var lines = ParserText.MessageLines(email);
        var results = new List<ParseResult>();
        var segmentStart = 0;

        for (var index = 0; index < lines.Count; index++)
        {
            var urlMatch = JobUrlRegex().Match(lines[index]);
            if (!urlMatch.Success)
            {
                continue;
            }

            var segment = lines.Skip(segmentStart).Take(index - segmentStart).ToArray();
            var applyIndex = Array.FindLastIndex(segment, IsApplyLine);
            if (applyIndex >= 2)
            {
                var (company, location) = SplitCompanyAndLocation(segment[applyIndex - 1]);
                if (company.Length > 0)
                {
                    var externalId = urlMatch.Groups["id"].Value;
                    var description = segment
                        .Skip(applyIndex + 1)
                        .TakeWhile(line => !IsAgeLine(line))
                        .Where(line => !line.StartsWith("https://", StringComparison.OrdinalIgnoreCase));

                    results.Add(CreateResult(
                        externalId,
                        segment[applyIndex - 2],
                        company,
                        new Uri($"https://{urlMatch.Groups["host"].Value}/viewjob?jk={externalId}"),
                        location: location,
                        description: string.Join(' ', description)));
                }
            }

            segmentStart = index + 1;
        }

        return results
            .DistinctBy(result => result.SourceExternalId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool IsApplyLine(string line) =>
        line.StartsWith("Aplikuj", StringComparison.OrdinalIgnoreCase) ||
        line.StartsWith("Apply", StringComparison.OrdinalIgnoreCase);

    private static bool IsAgeLine(string line) =>
        line.StartsWith("Dodano", StringComparison.OrdinalIgnoreCase) ||
        line.StartsWith("Posted", StringComparison.OrdinalIgnoreCase) ||
        line.StartsWith("Today", StringComparison.OrdinalIgnoreCase);

    private static (string Company, string? Location) SplitCompanyAndLocation(string value)
    {
        var separator = value.LastIndexOf(" - ", StringComparison.Ordinal);
        return separator < 0
            ? (ParserText.Clean(value), null)
            : (ParserText.Clean(value[..separator]), ParserText.NullIfEmpty(value[(separator + 3)..]));
    }

    [GeneratedRegex(@"https?://(?<host>(?:[a-z]{2}\.)?indeed\.com)/[^\s]*?[?&]jk=(?<id>[a-zA-Z0-9]+)", RegexOptions.IgnoreCase)]
    private static partial Regex JobUrlRegex();
}
