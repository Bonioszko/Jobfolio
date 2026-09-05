using System.Net.Http.Headers;
using App.Application;

namespace App.Infrastructure;

public abstract class SchemaOrgJobPostingDetailsFetcher(
    HttpClient httpClient,
    JobPostingDetailsFetcherOptions options) : IJobPostingDetailsFetcher
{
    private static readonly IReadOnlySet<string> HtmlMediaTypes = new HashSet<string>(
        ["text/html", "application/xhtml+xml"], StringComparer.OrdinalIgnoreCase);

    public abstract string SourceKey { get; }
    protected abstract string ProviderName { get; }

    public async Task<JobPostingDetails> FetchAsync(
        JobPostingDetailsReference reference,
        CancellationToken cancellationToken)
    {
        var url = ValidateAndNormalizeUrl(reference);
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
        using var response = await httpClient.SendAsync(
            request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        var html = await JobPostingHttpContentReader.ReadAsync(
            response.Content, options.MaxResponseBytes, HtmlMediaTypes, ProviderName, cancellationToken);
        return SchemaOrgJobPostingParser.Parse(html, ProviderName);
    }

    protected abstract Uri ValidateAndNormalizeUrl(JobPostingDetailsReference reference);

    protected static bool IsAllowedHttpsUrl(Uri url, string rootHost) =>
        url.IsAbsoluteUri && url.Scheme == Uri.UriSchemeHttps && url.IsDefaultPort &&
        url.UserInfo.Length == 0 &&
        (url.Host.Equals(rootHost, StringComparison.OrdinalIgnoreCase) ||
         url.Host.EndsWith($".{rootHost}", StringComparison.OrdinalIgnoreCase));

    protected static Uri WithoutTracking(Uri url) => new UriBuilder(url)
    {
        Query = string.Empty,
        Fragment = string.Empty
    }.Uri;
}
