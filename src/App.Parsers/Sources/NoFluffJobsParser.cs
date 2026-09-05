using System.Text;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using App.Application;

namespace App.Parsers.Sources;

public sealed class NoFluffJobsParser() : JobEmailParserBase("nofluffjobs.com")
{
    public override string Key => "nofluffjobs";

    protected override IReadOnlyList<ParseResult> Parse(EmailMessage email)
    {
        var document = new HtmlParser().ParseDocument(email.HtmlBody);
        var results = new List<ParseResult>();

        foreach (var card in document.QuerySelectorAll(".postings-table__item"))
        {
            var title = ParserText.NullIfEmpty(
                card.QuerySelector(".posting-content__title")?.TextContent);
            var anchor = card.QuerySelector("a[href]");
            var url = TryGetCanonicalUrl(anchor?.GetAttribute("href"));
            if (title is null || url is null)
            {
                continue;
            }

            var salaryElement = card.QuerySelector(".salary-label");
            var salary = ParserText.NullIfEmpty(salaryElement?.TextContent);
            if (salary?.Contains("salary match", StringComparison.OrdinalIgnoreCase) == true)
            {
                salary = null;
            }

            var details = card.QuerySelectorAll("span")
                .Where(element => !ReferenceEquals(element, salaryElement))
                .Select(element => ParserText.NullIfEmpty(element.TextContent))
                .Where(value => value is not null && !value.Equals(salary, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var externalId = Uri.UnescapeDataString(url.AbsolutePath.TrimEnd('/').Split('/').Last());
            results.Add(CreateResult(
                externalId,
                title,
                details.ElementAtOrDefault(0) ?? string.Empty,
                url,
                location: details.ElementAtOrDefault(1),
                salary: salary));
        }

        return results
            .DistinctBy(result => result.SourceExternalId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static Uri? TryGetCanonicalUrl(string? href)
    {
        if (!Uri.TryCreate(href, UriKind.Absolute, out var sourceUrl))
        {
            return null;
        }

        var jobUrl = IsNoFluffJobsUrl(sourceUrl) &&
                     sourceUrl.AbsolutePath.Contains("/job/", StringComparison.OrdinalIgnoreCase)
            ? sourceUrl
            : TryDecodeTrackedUrl(sourceUrl);
        if (jobUrl is null || !IsNoFluffJobsUrl(jobUrl) ||
            !jobUrl.AbsolutePath.Contains("/job/", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return new UriBuilder(jobUrl) { Query = string.Empty, Fragment = string.Empty }.Uri;
    }

    private static Uri? TryDecodeTrackedUrl(Uri trackingUrl)
    {
        foreach (var segment in trackingUrl.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var base64 = Uri.UnescapeDataString(segment).Replace('-', '+').Replace('_', '/');
                base64 = base64.PadRight(base64.Length + ((4 - base64.Length % 4) % 4), '=');
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
                if (Uri.TryCreate(decoded, UriKind.Absolute, out var candidate) && IsNoFluffJobsUrl(candidate))
                {
                    return candidate;
                }
            }
            catch (FormatException)
            {
                // Tracking paths contain several opaque segments; only one is the encoded target URL.
            }
        }

        return null;
    }

    private static bool IsNoFluffJobsUrl(Uri url) =>
        url.Host.Equals("nofluffjobs.com", StringComparison.OrdinalIgnoreCase) ||
        url.Host.EndsWith(".nofluffjobs.com", StringComparison.OrdinalIgnoreCase);
}
