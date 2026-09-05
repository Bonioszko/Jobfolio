using AngleSharp.Html.Parser;
using App.Application;

namespace App.Parsers.Sources;

public sealed class JustJoinItJobParser() : JobEmailParserBase("justjoin.it")
{
    public override string Key => "justjoin.it";

    protected override IReadOnlyList<ParseResult> Parse(EmailMessage email)
    {
        var document = new HtmlParser().ParseDocument(email.HtmlBody);
        var results = new List<ParseResult>();

        foreach (var anchor in document.QuerySelectorAll("a[href*='/job-offer/']"))
        {
            var url = TryGetCanonicalUrl(anchor.GetAttribute("href"));
            var title = ParserText.NullIfEmpty(anchor.QuerySelector(".offer-title")?.TextContent);
            var company = ParserText.NullIfEmpty(anchor.QuerySelector(".company-name")?.TextContent);
            if (url is null || title is null || company is null)
            {
                continue;
            }

            var details = anchor.QuerySelectorAll(".offer-details")
                .Select(element => ParserText.NullIfEmpty(element.TextContent))
                .Where(value => value is not null)
                .ToArray();
            var employmentType = details.FirstOrDefault(value =>
                !value!.Equals("Remote", StringComparison.OrdinalIgnoreCase) &&
                !value.Equals("Hybrid", StringComparison.OrdinalIgnoreCase) &&
                !value.Equals("Office", StringComparison.OrdinalIgnoreCase));
            var externalId = Uri.UnescapeDataString(url.AbsolutePath.TrimEnd('/').Split('/').Last());

            results.Add(CreateResult(
                externalId,
                title,
                company,
                url,
                location: ParserText.NullIfEmpty(anchor.QuerySelector(".company-city")?.TextContent),
                employmentType: employmentType,
                salary: ParserText.NullIfEmpty(anchor.QuerySelector(".salary")?.TextContent)));
        }

        return results
            .DistinctBy(result => result.SourceExternalId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static Uri? TryGetCanonicalUrl(string? href)
    {
        if (!Uri.TryCreate(href, UriKind.Absolute, out var url) ||
            !(url.Host.Equals("justjoin.it", StringComparison.OrdinalIgnoreCase) ||
              url.Host.EndsWith(".justjoin.it", StringComparison.OrdinalIgnoreCase)) ||
            !url.AbsolutePath.Contains("/job-offer/", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return new UriBuilder(url) { Query = string.Empty, Fragment = string.Empty }.Uri;
    }
}
