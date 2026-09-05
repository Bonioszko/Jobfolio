namespace App.Application;

public sealed class JobPostingDetailsEnricher(IEnumerable<IJobPostingDetailsFetcher> fetchers)
    : IJobPostingDetailsEnricher
{
    public async Task<ParseResult> EnrichAsync(
        ParseResult posting,
        CancellationToken cancellationToken)
    {
        var candidates = fetchers
            .Where(fetcher => fetcher.SourceKey.Equals(posting.SourceKey, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (candidates.Length != 1)
        {
            throw new InvalidOperationException(
                $"Expected one details fetcher for '{posting.SourceKey}', but found {candidates.Length}.");
        }

        if (!posting.ParsedData.TryGetValue("url", out var urlValue) ||
            urlValue is not string urlText ||
            !Uri.TryCreate(urlText, UriKind.Absolute, out var url))
        {
            throw new FormatException("The parsed posting does not contain a valid source URL.");
        }

        JobPostingDetails details;
        try
        {
            details = await candidates[0].FetchAsync(
                new JobPostingDetailsReference(posting.SourceKey, posting.SourceExternalId, url),
                cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return posting;
        }
        catch (HttpRequestException)
        {
            return posting;
        }
        catch (IOException)
        {
            return posting;
        }
        catch (FormatException)
        {
            // Provider pages are best-effort enrichment. Keep the deterministic email
            // fields, including a missing description, when page details are unavailable.
            return posting;
        }

        var parsedData = new Dictionary<string, object?>(posting.ParsedData);
        parsedData["description"] = details.Description;
        Merge(parsedData, "location", details.Location);
        Merge(parsedData, "employmentType", details.EmploymentType);
        Merge(parsedData, "salary", details.Salary);

        var searchData = new Dictionary<string, object?>(posting.SearchData);
        Merge(searchData, "location", details.Location);
        Merge(searchData, "employmentType", details.EmploymentType);

        return posting with { ParsedData = parsedData, SearchData = searchData };
    }

    private static void Merge(Dictionary<string, object?> target, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) target[key] = value;
    }
}
