using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using App.Application;

namespace App.Infrastructure;

internal static partial class SchemaOrgJobPostingParser
{
    public static JobPostingDetails Parse(string html, string providerName)
    {
        var document = new HtmlParser().ParseDocument(html);
        foreach (var script in document.QuerySelectorAll("script[type='application/ld+json']"))
        {
            try
            {
                using var json = JsonDocument.Parse(script.TextContent);
                if (TryFindJobPosting(json.RootElement, out var posting))
                {
                    return CreateDetails(posting, providerName);
                }
            }
            catch (JsonException)
            {
                // Ignore unrelated malformed structured-data blocks.
            }
        }

        throw new FormatException($"The {providerName} page did not contain JobPosting structured data.");
    }

    private static JobPostingDetails CreateDetails(JsonElement posting, string providerName)
    {
        var description = GetText(posting, "description");
        if (description is null)
        {
            throw new FormatException($"The {providerName} page did not contain a usable description.");
        }

        return new JobPostingDetails(
            description,
            GetLocation(posting),
            GetText(posting, "employmentType"),
            GetSalary(posting));
    }

    private static bool TryFindJobPosting(JsonElement element, out JsonElement posting)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                if (TryFindJobPosting(item, out posting)) return true;
            }
        }
        else if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("@type", out var type) && IsJobPostingType(type))
            {
                posting = element;
                return true;
            }

            if (element.TryGetProperty("@graph", out var graph) && TryFindJobPosting(graph, out posting))
            {
                return true;
            }
        }

        posting = default;
        return false;
    }

    private static bool IsJobPostingType(JsonElement type) =>
        type.ValueKind == JsonValueKind.String
            ? type.GetString()?.Equals("JobPosting", StringComparison.OrdinalIgnoreCase) == true
            : type.ValueKind == JsonValueKind.Array && type.EnumerateArray().Any(IsJobPostingType);

    private static string? GetText(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value)) return null;
        var values = EnumerateText(value)
            .Select(CleanHtml)
            .Where(item => item.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase);
        return NullIfEmpty(string.Join(", ", values));
    }

    private static IEnumerable<string> EnumerateText(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.String)
        {
            yield return value.GetString() ?? string.Empty;
        }
        else if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in value.EnumerateArray())
            foreach (var text in EnumerateText(item))
                yield return text;
        }
    }

    private static string? GetLocation(JsonElement posting)
    {
        var locations = new List<string>();
        if (posting.TryGetProperty("jobLocationType", out var locationType) &&
            EnumerateText(locationType).Any(value => value.Equals("TELECOMMUTE", StringComparison.OrdinalIgnoreCase)))
        {
            locations.Add("Remote");
        }

        if (posting.TryGetProperty("jobLocation", out var jobLocation))
        {
            locations.AddRange(EnumerateLocations(jobLocation));
        }

        return NullIfEmpty(string.Join("; ", locations.Distinct(StringComparer.OrdinalIgnoreCase)));
    }

    private static IEnumerable<string> EnumerateLocations(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in value.EnumerateArray())
            foreach (var location in EnumerateLocations(item))
                yield return location;
            yield break;
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            var text = Clean(value.GetString());
            if (text.Length > 0) yield return text;
            yield break;
        }

        if (value.ValueKind != JsonValueKind.Object) yield break;
        if (value.TryGetProperty("address", out var address))
        {
            var parts = address.ValueKind == JsonValueKind.String
                ? [Clean(address.GetString())]
                : new[] { GetRawText(address, "addressLocality"), GetRawText(address, "addressRegion"), GetCountry(address) };
            var formatted = NullIfEmpty(string.Join(", ", parts.Where(part => !string.IsNullOrWhiteSpace(part))));
            if (formatted is not null)
            {
                yield return formatted;
                yield break;
            }
        }

        var name = GetRawText(value, "name");
        if (name is not null) yield return name;
    }

    private static string? GetCountry(JsonElement address)
    {
        if (!address.TryGetProperty("addressCountry", out var country)) return null;
        return country.ValueKind == JsonValueKind.Object
            ? GetRawText(country, "name")
            : country.ValueKind == JsonValueKind.String ? Clean(country.GetString()) : null;
    }

    private static string? GetSalary(JsonElement posting)
    {
        if (!posting.TryGetProperty("baseSalary", out var salary)) return null;
        if (salary.ValueKind == JsonValueKind.String) return NullIfEmpty(salary.GetString());
        if (salary.ValueKind != JsonValueKind.Object) return null;

        var currency = GetRawText(salary, "currency");
        if (!salary.TryGetProperty("value", out var value)) return currency;
        var amount = value.ValueKind switch
        {
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Object => FormatSalaryValue(value),
            _ => null
        };
        return NullIfEmpty($"{amount} {currency}");
    }

    private static string? FormatSalaryValue(JsonElement value)
    {
        var exact = GetRawText(value, "value");
        var minimum = GetRawText(value, "minValue");
        var maximum = GetRawText(value, "maxValue");
        var amount = exact ?? (minimum is not null && maximum is not null
            ? $"{minimum}–{maximum}"
            : minimum ?? maximum);
        return NullIfEmpty($"{amount} {GetRawText(value, "unitText")}");
    }

    private static string? GetRawText(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value)) return null;
        return value.ValueKind switch
        {
            JsonValueKind.String => NullIfEmpty(value.GetString()),
            JsonValueKind.Number => value.GetRawText(),
            _ => null
        };
    }

    private static string CleanHtml(string value)
    {
        var parser = new HtmlParser();
        var host = parser.ParseDocument("<body></body>").Body!;
        return Clean(string.Join('\n', parser.ParseFragment(value, host).Select(node => node.TextContent)));
    }

    private static string? NullIfEmpty(string? value)
    {
        var cleaned = Clean(value);
        return cleaned.Length == 0 ? null : cleaned;
    }

    private static string Clean(string? value) =>
        WhitespaceRegex().Replace(WebUtility.HtmlDecode(value ?? string.Empty), " ").Trim();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
