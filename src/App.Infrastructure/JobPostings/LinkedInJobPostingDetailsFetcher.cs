using System.Text.RegularExpressions;
using App.Application;

namespace App.Infrastructure;

public sealed partial class LinkedInJobPostingDetailsFetcher(
    HttpClient httpClient,
    JobPostingDetailsFetcherOptions options)
    : SchemaOrgJobPostingDetailsFetcher(httpClient, options)
{
    public override string SourceKey => "linkedin";
    protected override string ProviderName => "LinkedIn";

    protected override Uri ValidateAndNormalizeUrl(JobPostingDetailsReference reference)
    {
        var match = JobPathRegex().Match(reference.Url.AbsolutePath);
        if (!reference.SourceKey.Equals(SourceKey, StringComparison.OrdinalIgnoreCase) ||
            !IsAllowedHttpsUrl(reference.Url, "linkedin.com") || !match.Success ||
            !match.Groups["id"].Value.Equals(reference.SourceExternalId, StringComparison.Ordinal))
        {
            throw new ArgumentException("The LinkedIn job URL or external identifier is invalid.", nameof(reference));
        }

        return WithoutTracking(reference.Url);
    }

    [GeneratedRegex(@"^/(?:comm/)?jobs/view/(?:[^/]*-)?(?<id>\d+)(?:/|$)", RegexOptions.IgnoreCase)]
    private static partial Regex JobPathRegex();
}
