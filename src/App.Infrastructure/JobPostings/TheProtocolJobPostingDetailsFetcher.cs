using System.Text.RegularExpressions;
using App.Application;

namespace App.Infrastructure;

public sealed partial class TheProtocolJobPostingDetailsFetcher(
    HttpClient httpClient,
    JobPostingDetailsFetcherOptions options)
    : SchemaOrgJobPostingDetailsFetcher(httpClient, options)
{
    public override string SourceKey => "theprotocol.it";
    protected override string ProviderName => "the:protocol";

    protected override Uri ValidateAndNormalizeUrl(JobPostingDetailsReference reference)
    {
        var match = JobPathRegex().Match(reference.Url.AbsolutePath);
        if (!reference.SourceKey.Equals(SourceKey, StringComparison.OrdinalIgnoreCase) ||
            !IsAllowedHttpsUrl(reference.Url, "theprotocol.it") ||
            !match.Success ||
            !match.Groups["id"].Value.Equals(
                reference.SourceExternalId,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "The the:protocol job URL or external identifier is invalid.",
                nameof(reference));
        }

        return WithoutTracking(reference.Url);
    }

    [GeneratedRegex(
        @"^/szczegoly/praca/.+,oferta,(?<id>[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})(?:/|$)",
        RegexOptions.IgnoreCase)]
    private static partial Regex JobPathRegex();
}
