using App.Application;

namespace App.Infrastructure;

public sealed class IndeedJobPostingDetailsFetcher(
    HttpClient httpClient,
    JobPostingDetailsFetcherOptions options)
    : SchemaOrgJobPostingDetailsFetcher(httpClient, options)
{
    public override string SourceKey => "indeed";
    protected override string ProviderName => "Indeed";

    protected override Uri ValidateAndNormalizeUrl(JobPostingDetailsReference reference)
    {
        var jobKey = reference.Url.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .Where(part => part.Length == 2 && part[0].Equals("jk", StringComparison.OrdinalIgnoreCase))
            .Select(part => Uri.UnescapeDataString(part[1]))
            .SingleOrDefault();
        if (!reference.SourceKey.Equals(SourceKey, StringComparison.OrdinalIgnoreCase) ||
            !IsAllowedHttpsUrl(reference.Url, "indeed.com") ||
            !reference.Url.AbsolutePath.Equals("/viewjob", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(jobKey, reference.SourceExternalId, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The Indeed job URL or external identifier is invalid.", nameof(reference));
        }

        return new UriBuilder(reference.Url)
        {
            Query = $"jk={Uri.EscapeDataString(reference.SourceExternalId)}",
            Fragment = string.Empty
        }.Uri;
    }
}
