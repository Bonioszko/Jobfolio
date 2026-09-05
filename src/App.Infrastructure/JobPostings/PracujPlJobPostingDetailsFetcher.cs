using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using App.Application;

namespace App.Infrastructure;

public sealed partial class PracujPlJobPostingDetailsFetcher(
    HttpClient httpClient,
    JobPostingDetailsFetcherOptions options) : IJobPostingDetailsFetcher
{
    public const int DefaultMaxResponseBytes = 2_000_000;
    public const int DefaultTimeoutSeconds = 15;

    private static readonly IReadOnlySet<string> JsonMediaTypes = new HashSet<string>(
        ["application/json", "text/json"], StringComparer.OrdinalIgnoreCase);
    private static readonly IReadOnlyDictionary<string, string> SectionLabels =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["about-project"] = "About the project",
            ["responsibilities"] = "Responsibilities",
            ["requirements-expected"] = "Requirements",
            ["requirements-optional"] = "Nice to have",
            ["technologies-expected"] = "Technologies",
            ["technologies-optional"] = "Optional technologies",
            ["offered"] = "What is offered",
            ["development-practices"] = "Development practices"
        };

    public string SourceKey => "pracuj.pl";

    public async Task<JobPostingDetails> FetchAsync(
        JobPostingDetailsReference reference,
        CancellationToken cancellationToken)
    {
        ValidateReference(reference);
        var apiUrl = new Uri(
            $"https://massachusetts.pracuj.pl/offer/{Uri.EscapeDataString(reference.SourceExternalId)}/composed?languageCode=pl");
        using var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var response = await httpClient.SendAsync(
            request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await JobPostingHttpContentReader.ReadAsync(
            response.Content, options.MaxResponseBytes, JsonMediaTypes, "Pracuj.pl", cancellationToken);
        return Parse(json, reference.SourceExternalId);
    }

    private static JobPostingDetails Parse(string json, string expectedId)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("offer", out var offer) ||
            !offer.TryGetProperty("jobOfferWebId", out var id) || id.GetRawText() != expectedId)
        {
            throw new FormatException("The Pracuj.pl response did not match the requested posting.");
        }

        var description = GetDescription(offer);
        if (description.Length == 0)
        {
            throw new FormatException("The Pracuj.pl response did not contain a usable description.");
        }

        offer.TryGetProperty("attributes", out var attributes);
        attributes.TryGetProperty("employment", out var employment);
        return new JobPostingDetails(
            description,
            GetLocation(attributes, employment),
            GetContracts(employment),
            GetSalaries(employment));
    }

    private static string GetDescription(JsonElement offer)
    {
        if (!offer.TryGetProperty("textSections", out var sections) || sections.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        var result = new List<string>();
        foreach (var section in sections.EnumerateArray())
        {
            var type = GetString(section, "sectionType");
            if (type is null || !SectionLabels.TryGetValue(type, out var label)) continue;

            var elements = section.TryGetProperty("textElements", out var values) && values.ValueKind == JsonValueKind.Array
                ? values.EnumerateArray()
                    .Where(value => value.ValueKind == JsonValueKind.String)
                    .Select(value => Clean(value.GetString()))
                    .Where(value => value.Length > 0)
                    .ToArray()
                : [];
            if (elements.Length == 0 && GetString(section, "plainText") is { } plainText)
            {
                elements = [plainText];
            }

            if (elements.Length > 0)
            {
                result.Add(elements.Length == 1
                    ? $"{label}:\n{elements[0]}"
                    : $"{label}:\n{string.Join("\n", elements.Select(value => $"- {value}"))}");
            }
        }

        return string.Join("\n\n", result.Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private static string? GetLocation(JsonElement attributes, JsonElement employment)
    {
        if (employment.ValueKind == JsonValueKind.Object &&
            employment.TryGetProperty("entirelyRemoteWork", out var remote) && remote.ValueKind == JsonValueKind.True)
        {
            return "Cała Polska (praca zdalna)";
        }

        if (attributes.ValueKind != JsonValueKind.Object ||
            !attributes.TryGetProperty("workplaces", out var workplaces) || workplaces.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        return NullIfEmpty(string.Join("; ", workplaces.EnumerateArray()
            .Select(workplace => GetString(workplace, "displayAddress") ??
                                 GetNestedString(workplace, "inlandLocation", "location", "name"))
            .Where(value => value is not null)
            .Distinct(StringComparer.OrdinalIgnoreCase)));
    }

    private static string? GetContracts(JsonElement employment) =>
        NullIfEmpty(string.Join(", ", GetContractElements(employment)
            .Select(contract => GetString(contract, "pracujPlName") ?? GetString(contract, "name"))
            .Where(value => value is not null)
            .Distinct(StringComparer.OrdinalIgnoreCase)));

    private static string? GetSalaries(JsonElement employment) =>
        NullIfEmpty(string.Join("; ", GetContractElements(employment)
            .Select(FormatSalary)
            .Where(value => value is not null)
            .Distinct(StringComparer.OrdinalIgnoreCase)));

    private static IEnumerable<JsonElement> GetContractElements(JsonElement employment) =>
        employment.ValueKind == JsonValueKind.Object &&
        employment.TryGetProperty("typesOfContracts", out var contracts) && contracts.ValueKind == JsonValueKind.Array
            ? contracts.EnumerateArray().ToArray()
            : [];

    private static string? FormatSalary(JsonElement contract)
    {
        if (!contract.TryGetProperty("salary", out var salary) || salary.ValueKind != JsonValueKind.Object) return null;
        var from = GetDecimal(salary, "from");
        var to = GetDecimal(salary, "to");
        var amount = from is not null && to is not null
            ? $"{FormatAmount(from.Value)} – {FormatAmount(to.Value)}"
            : from is not null ? $"od {FormatAmount(from.Value)}" : to is not null ? $"do {FormatAmount(to.Value)}" : null;
        var currency = GetNestedString(salary, "currency", "symbol") ?? GetNestedString(salary, "currency", "code");
        var kind = GetNestedString(salary, "salaryKind", "pracujPlName") ?? GetNestedString(salary, "salaryKind", "name");
        var unit = GetNestedString(salary, "timeUnit", "shortForm", "pracujPlName") ??
                   GetNestedString(salary, "timeUnit", "shortForm", "name");
        var contractName = GetString(contract, "pracujPlName") ?? GetString(contract, "name");
        return NullIfEmpty($"{amount} {currency} {kind}{(unit is null ? null : $" / {unit}")}{(contractName is null ? null : $" | {contractName}")}");
    }

    private static string FormatAmount(decimal amount) =>
        amount.ToString("N0", CultureInfo.GetCultureInfo("pl-PL")).Replace('\u00a0', ' ');

    private static decimal? GetDecimal(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) && value.TryGetDecimal(out var number) ? number : null;

    private static string? GetNestedString(JsonElement element, params string[] path)
    {
        foreach (var part in path)
        {
            if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(part, out element)) return null;
        }

        return element.ValueKind == JsonValueKind.String ? NullIfEmpty(element.GetString()) : null;
    }

    private static string? GetString(JsonElement element, string propertyName) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var value) &&
        value.ValueKind == JsonValueKind.String ? NullIfEmpty(value.GetString()) : null;

    private static string? NullIfEmpty(string? value)
    {
        var cleaned = Clean(value);
        return cleaned.Length == 0 ? null : cleaned;
    }

    private static string Clean(string? value) => WhitespaceRegex().Replace(value ?? string.Empty, " ").Trim();

    private static void ValidateReference(JobPostingDetailsReference reference)
    {
        var url = reference.Url;
        var allowedHost = url.Host.Equals("pracuj.pl", StringComparison.OrdinalIgnoreCase) ||
                          url.Host.EndsWith(".pracuj.pl", StringComparison.OrdinalIgnoreCase);
        var match = JobPathRegex().Match(url.AbsolutePath);
        if (!reference.SourceKey.Equals("pracuj.pl", StringComparison.OrdinalIgnoreCase) ||
            !url.IsAbsoluteUri || url.Scheme != Uri.UriSchemeHttps || !url.IsDefaultPort ||
            url.UserInfo.Length > 0 || !allowedHost || !match.Success ||
            !match.Groups["id"].Value.Equals(reference.SourceExternalId, StringComparison.Ordinal))
        {
            throw new ArgumentException("The Pracuj.pl job URL or external identifier is invalid.", nameof(reference));
        }
    }

    [GeneratedRegex(@"^/praca/.+,oferta,(?<id>\d+)(?:/|$)", RegexOptions.IgnoreCase)]
    private static partial Regex JobPathRegex();
    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
