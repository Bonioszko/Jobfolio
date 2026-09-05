using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using App.Application;

namespace App.Parsers.Sources;

public sealed partial class PracujPlJobParser() : JobEmailParserBase("pracuj.pl")
{
    public override string Key => "pracuj.pl";
    public override int Version => 1;

    protected override IReadOnlyList<ParseResult> Parse(EmailMessage email)
    {
        var document = new HtmlParser().ParseDocument(email.HtmlBody);
        var results = new List<ParseResult>();

        foreach (var card in document.QuerySelectorAll(".desktop_padding_offer"))
        {
            var titleAnchor = card.QuerySelector(
                "a.anchor[href]:not(.employer_color):not(.salary_color)");
            var companyAnchor = card.QuerySelector("a.anchor.employer_color[href]");
            var jobUrl = TryGetCanonicalUrl(titleAnchor?.GetAttribute("href"));
            var title = GetTitle(titleAnchor);
            var company = ParserText.NullIfEmpty(companyAnchor?.TextContent);

            if (jobUrl is null || title is null || company is null)
            {
                continue;
            }

            results.Add(CreateResult(
                jobUrl.Value.ExternalId,
                title,
                company,
                jobUrl.Value.Url,
                location: ParserText.NullIfEmpty(card.QuerySelector(".workplace")?.TextContent),
                salary: ParserText.NullIfEmpty(card.QuerySelector("a.salary_color")?.TextContent)));
        }

        return results
            .DistinctBy(result => result.SourceExternalId, StringComparer.Ordinal)
            .ToArray();
    }

    private static string? GetTitle(IElement? anchor)
    {
        var title = ParserText.NullIfEmpty(anchor?.TextContent);
        var marker = ParserText.NullIfEmpty(
            anchor?.QuerySelector(".exclamation_mark_icon")?.TextContent);

        if (title is not null && marker is not null &&
            title.StartsWith(marker, StringComparison.Ordinal))
        {
            title = ParserText.NullIfEmpty(title[marker.Length..]);
        }

        return title;
    }

    private static (Uri Url, string ExternalId)? TryGetCanonicalUrl(string? href)
    {
        if (!Uri.TryCreate(href, UriKind.Absolute, out var sourceUrl) ||
            !IsPracujPlUrl(sourceUrl))
        {
            return null;
        }

        var match = JobPathRegex().Match(sourceUrl.AbsolutePath);
        if (!match.Success)
        {
            return null;
        }

        var url = new UriBuilder(sourceUrl)
        {
            Query = string.Empty,
            Fragment = string.Empty
        }.Uri;

        return (url, match.Groups["id"].Value);
    }

    private static bool IsPracujPlUrl(Uri url) =>
        url.Host.Equals("pracuj.pl", StringComparison.OrdinalIgnoreCase) ||
        url.Host.EndsWith(".pracuj.pl", StringComparison.OrdinalIgnoreCase);

    [GeneratedRegex(@"^/praca/.+,oferta,(?<id>\d+)(?:/|$)", RegexOptions.IgnoreCase)]
    private static partial Regex JobPathRegex();
}
