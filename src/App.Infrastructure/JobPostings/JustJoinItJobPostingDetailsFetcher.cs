using App.Application;

namespace App.Infrastructure;

public sealed class JustJoinItJobPostingDetailsFetcher(
    HttpClient httpClient,
    JobPostingDetailsFetcherOptions options)
    : SchemaOrgJobPostingDetailsFetcher(httpClient, options)
{
    public override string SourceKey => "justjoin.it";
    protected override string ProviderName => "Just Join IT";

    protected override Uri ValidateAndNormalizeUrl(JobPostingDetailsReference reference)
    {
        var segments = reference.Url.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var externalId = segments.Length == 2 && segments[0].Equals("job-offer", StringComparison.OrdinalIgnoreCase)
            ? Uri.UnescapeDataString(segments[1])
            : null;
        if (!reference.SourceKey.Equals(SourceKey, StringComparison.OrdinalIgnoreCase) ||
            !IsAllowedHttpsUrl(reference.Url, "justjoin.it") ||
            !string.Equals(externalId, reference.SourceExternalId, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The Just Join IT job URL or external identifier is invalid.", nameof(reference));
        }

        return WithoutTracking(reference.Url);
    }
}
