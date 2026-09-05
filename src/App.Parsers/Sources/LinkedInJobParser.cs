using System.Text.RegularExpressions;
using App.Application;

namespace App.Parsers.Sources;

public sealed partial class LinkedInJobParser() : JobEmailParserBase("linkedin.com")
{
    public override string Key => "linkedin";

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
                if (SeparatorRegex().IsMatch(lines[index]))
                {
                    segmentStart = index + 1;
                }

                continue;
            }

            var fields = lines
                .Skip(segmentStart)
                .Take(index - segmentStart)
                .Where(IsPostingField)
                .TakeLast(3)
                .ToArray();
            if (fields.Length == 3)
            {
                var externalId = urlMatch.Groups["id"].Value;
                results.Add(CreateResult(
                    externalId,
                    fields[0],
                    fields[1],
                    new Uri($"https://www.linkedin.com/jobs/view/{externalId}"),
                    location: fields[2]));
            }

            segmentStart = index + 1;
        }

        return results
            .DistinctBy(result => result.SourceExternalId, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool IsPostingField(string line) =>
        !line.StartsWith("View job", StringComparison.OrdinalIgnoreCase) &&
        !line.StartsWith("Apply with", StringComparison.OrdinalIgnoreCase) &&
        !line.Contains("company alumni", StringComparison.OrdinalIgnoreCase) &&
        !line.Equals("New jobs match your preferences.", StringComparison.OrdinalIgnoreCase) &&
        !line.StartsWith("Your job alert", StringComparison.OrdinalIgnoreCase) &&
        !SeparatorRegex().IsMatch(line);

    [GeneratedRegex(@"https?://(?:[a-z]+\.)?linkedin\.com/(?:comm/)?jobs/view/(?<id>\d+)(?:[/?#]|$)", RegexOptions.IgnoreCase)]
    private static partial Regex JobUrlRegex();

    [GeneratedRegex(@"^-{5,}$")]
    private static partial Regex SeparatorRegex();
}
