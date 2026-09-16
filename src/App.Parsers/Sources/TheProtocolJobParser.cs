using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using App.Application;

namespace App.Parsers.Sources;

public sealed partial class TheProtocolJobParser() : JobEmailParserBase("theprotocol.it")
{
    public override string Key => "theprotocol.it";
    public override int Version => 1;

    protected override IReadOnlyList<ParseResult> Parse(EmailMessage email)
    {
        var document = new HtmlParser().ParseDocument(email.HtmlBody);
        var links = new List<JobLink>();
        foreach (var anchor in document.QuerySelectorAll("a[href]"))
        {
            if (TryGetCanonicalUrl(anchor.GetAttribute("href")) is { } jobUrl)
            {
                links.Add(new JobLink(anchor, jobUrl.Url, jobUrl.ExternalId));
            }
        }

        var results = new List<ParseResult>();

        foreach (var group in links.GroupBy(
                     candidate => candidate.ExternalId,
                     StringComparer.OrdinalIgnoreCase))
        {
            var candidates = group.ToArray();
            var title = GetField(candidates, "primary_blue_color");
            var companyAndLocation = GetField(candidates, "primary_color");
            var (company, location) = SplitCompanyAndLocation(companyAndLocation);
            if (title is null || company is null)
            {
                continue;
            }

            var jobUrl = candidates[0];
            results.Add(CreateResult(
                jobUrl.ExternalId,
                title,
                company,
                jobUrl.Url,
                location: location,
                salary: GetField(candidates, "salary_color")));
        }

        return results;
    }

    private static string? GetField(IEnumerable<JobLink> candidates, string cellClass)
    {
        foreach (var candidate in candidates)
        {
            if (candidate.Anchor.Closest("td")?.ClassList.Contains(cellClass) == true)
            {
                return ParserText.NullIfEmpty(candidate.Anchor.TextContent);
            }
        }

        return null;
    }

    private static (string? Company, string? Location) SplitCompanyAndLocation(string? value)
    {
        if (value is null)
        {
            return (null, null);
        }

        var separatorIndex = value.LastIndexOf(" - ", StringComparison.Ordinal);
        return separatorIndex < 0
            ? (ParserText.NullIfEmpty(value), null)
            : (
                ParserText.NullIfEmpty(value[..separatorIndex]),
                ParserText.NullIfEmpty(value[(separatorIndex + 3)..]));
    }

    private static (Uri Url, string ExternalId)? TryGetCanonicalUrl(string? href)
    {
        if (!Uri.TryCreate(href, UriKind.Absolute, out var sourceUrl) ||
            sourceUrl.Scheme != Uri.UriSchemeHttps ||
            !sourceUrl.IsDefaultPort ||
            sourceUrl.UserInfo.Length > 0 ||
            !IsTheProtocolUrl(sourceUrl))
        {
            return null;
        }

        var match = JobPathRegex().Match(sourceUrl.AbsolutePath);
        if (!match.Success || !Guid.TryParseExact(match.Groups["id"].Value, "D", out var externalId))
        {
            return null;
        }

        var url = new UriBuilder(sourceUrl)
        {
            Query = string.Empty,
            Fragment = string.Empty
        }.Uri;

        return (url, externalId.ToString("D"));
    }

    private static bool IsTheProtocolUrl(Uri url) =>
        url.Host.Equals("theprotocol.it", StringComparison.OrdinalIgnoreCase) ||
        url.Host.EndsWith(".theprotocol.it", StringComparison.OrdinalIgnoreCase);

    [GeneratedRegex(
        @"^/szczegoly/praca/.+,oferta,(?<id>[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})(?:/|$)",
        RegexOptions.IgnoreCase)]
    private static partial Regex JobPathRegex();

    private sealed record JobLink(IElement Anchor, Uri Url, string ExternalId);
}
