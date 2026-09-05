using App.Application;

namespace App.Infrastructure;

public sealed class NoFluffJobsJobPostingDetailsFetcher(
    HttpClient httpClient,
    JobPostingDetailsFetcherOptions options)
    : SchemaOrgJobPostingDetailsFetcher(httpClient, options)
{
    public override string SourceKey => "nofluffjobs";
    protected override string ProviderName => "No Fluff Jobs";

    protected override Uri ValidateAndNormalizeUrl(JobPostingDetailsReference reference)
    {
        var segments = reference.Url.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var jobIndex = Array.FindIndex(segments, value => value.Equals("job", StringComparison.OrdinalIgnoreCase));
        var externalId = jobIndex >= 0 && jobIndex == segments.Length - 2
            ? Uri.UnescapeDataString(segments[^1])
            : null;
        if (!reference.SourceKey.Equals(SourceKey, StringComparison.OrdinalIgnoreCase) ||
            !IsAllowedHttpsUrl(reference.Url, "nofluffjobs.com") ||
            !string.Equals(externalId, reference.SourceExternalId, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The No Fluff Jobs URL or external identifier is invalid.", nameof(reference));
        }

        return WithoutTracking(reference.Url);
    }
}
