using System.Net;
using System.Text;
using App.Application;
using App.Infrastructure;

namespace App.Tests;

public sealed class JobPostingDetailsFetcherTests
{
    public static TheoryData<string, string, string, string, string> SchemaProviders => new()
    {
        { "linkedin", "4450756772", "https://www.linkedin.com/jobs/view/4450756772?trk=email", "linkedin-offer.html", "Build reliable .NET services." },
        { "justjoin.it", "example-net-developer", "https://justjoin.it/job-offer/example-net-developer?utm_source=email", "justjoinit-offer.html", "Develop a logistics platform" },
        { "nofluffjobs", "example-net-developer", "https://nofluffjobs.com/pl/job/example-net-developer?utm_source=email", "nofluffjobs-offer.html", "Strong C# and Azure experience." },
        { "indeed", "abc123", "https://pl.indeed.com/viewjob?jk=abc123&from=job-alert", "indeed-offer.html", "Maintain backend APIs" },
        { "theprotocol.it", "11111111-1111-4111-8111-111111111111", "https://theprotocol.it/szczegoly/praca/fullstack-product-engineer,oferta,11111111-1111-4111-8111-111111111111?utm_source=jobalert", "theprotocol-offer.html", "Build reliable .NET services" }
    };

    [Theory]
    [MemberData(nameof(SchemaProviders))]
    public async Task Schema_provider_fetchers_populate_description_and_normalized_fields(
        string sourceKey,
        string externalId,
        string url,
        string fixture,
        string expectedDescription)
    {
        var handler = SuccessHandler(await ReadFixtureAsync(fixture), "text/html");
        var fetcher = CreateFetcher(sourceKey, handler);

        var details = await fetcher.FetchAsync(
            new JobPostingDetailsReference(sourceKey, externalId, new Uri(url)),
            CancellationToken.None);

        Assert.Contains(expectedDescription, details.Description);
        Assert.NotNull(handler.RequestUri);
        Assert.DoesNotContain("utm_", handler.RequestUri.Query, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("from=", handler.RequestUri.Query, StringComparison.OrdinalIgnoreCase);
        if (sourceKey == "justjoin.it")
        {
            Assert.Equal("Remote", details.Location);
            Assert.Equal("CONTRACTOR", details.EmploymentType);
            Assert.Equal("14000–23000 MONTH PLN", details.Salary);
        }
    }

    [Theory]
    [InlineData("linkedin", "4450756772", "https://linkedin.com.attacker.test/jobs/view/4450756772")]
    [InlineData("justjoin.it", "example-net-developer", "https://justjoin.it.attacker.test/job-offer/example-net-developer")]
    [InlineData("nofluffjobs", "example-net-developer", "https://nofluffjobs.com.attacker.test/job/example-net-developer")]
    [InlineData("indeed", "abc123", "https://indeed.com.attacker.test/viewjob?jk=abc123")]
    [InlineData("theprotocol.it", "11111111-1111-4111-8111-111111111111", "https://theprotocol.it.attacker.test/szczegoly/praca/developer,oferta,11111111-1111-4111-8111-111111111111")]
    public async Task Schema_provider_fetchers_reject_untrusted_hosts(
        string sourceKey,
        string externalId,
        string url)
    {
        var handler = SuccessHandler("", "text/html");
        var fetcher = CreateFetcher(sourceKey, handler);

        await Assert.ThrowsAsync<ArgumentException>(() => fetcher.FetchAsync(
            new JobPostingDetailsReference(sourceKey, externalId, new Uri(url)),
            CancellationToken.None));
        Assert.Null(handler.RequestUri);
    }

    [Fact]
    public async Task TheProtocol_fetcher_rejects_an_identifier_that_does_not_match_the_page_url()
    {
        var handler = SuccessHandler("", "text/html");
        var fetcher = CreateFetcher("theprotocol.it", handler);

        await Assert.ThrowsAsync<ArgumentException>(() => fetcher.FetchAsync(
            new JobPostingDetailsReference(
                "theprotocol.it",
                "99999999-9999-4999-8999-999999999999",
                new Uri("https://theprotocol.it/szczegoly/praca/developer,oferta,11111111-1111-4111-8111-111111111111")),
            CancellationToken.None));
        Assert.Null(handler.RequestUri);
    }

    [Fact]
    public async Task Pracuj_fetcher_uses_public_composed_offer_api_and_populates_all_details()
    {
        var handler = SuccessHandler(await ReadFixtureAsync("pracujpl-offer.json"), "application/json");
        var fetcher = CreateFetcher("pracuj.pl", handler);

        var details = await fetcher.FetchAsync(
            new JobPostingDetailsReference(
                "pracuj.pl",
                "1000000002",
                new Uri("https://www.pracuj.pl/praca/net-developer-warszawa,oferta,1000000002?utm_source=email")),
            CancellationToken.None);

        Assert.Equal(
            "https://massachusetts.pracuj.pl/offer/1000000002/composed?languageCode=pl",
            handler.RequestUri?.AbsoluteUri);
        Assert.Contains("Responsibilities:\n- Projektowanie usług backendowych.", details.Description);
        Assert.Contains("Requirements:\nBardzo dobra znajomość C#.", details.Description);
        Assert.DoesNotContain("Ignored non-job benefit", details.Description);
        Assert.Equal("Cała Polska (praca zdalna)", details.Location);
        Assert.Equal("kontrakt B2B", details.EmploymentType);
        Assert.Equal("14 000 – 23 000 zł netto (+ VAT) / mies. | kontrakt B2B", details.Salary);
    }

    [Fact]
    public async Task Pracuj_fetcher_rejects_an_identifier_that_does_not_match_the_page_url()
    {
        var handler = SuccessHandler("{}", "application/json");
        var fetcher = CreateFetcher("pracuj.pl", handler);

        await Assert.ThrowsAsync<ArgumentException>(() => fetcher.FetchAsync(
            new JobPostingDetailsReference(
                "pracuj.pl",
                "different-id",
                new Uri("https://www.pracuj.pl/praca/developer,oferta,1000000002")),
            CancellationToken.None));
        Assert.Null(handler.RequestUri);
    }

    private static IJobPostingDetailsFetcher CreateFetcher(string sourceKey, HttpMessageHandler handler)
    {
        var client = new HttpClient(handler);
        var options = new JobPostingDetailsFetcherOptions(PracujPlJobPostingDetailsFetcher.DefaultMaxResponseBytes);
        return sourceKey switch
        {
            "linkedin" => new LinkedInJobPostingDetailsFetcher(client, options),
            "justjoin.it" => new JustJoinItJobPostingDetailsFetcher(client, options),
            "nofluffjobs" => new NoFluffJobsJobPostingDetailsFetcher(client, options),
            "indeed" => new IndeedJobPostingDetailsFetcher(client, options),
            "pracuj.pl" => new PracujPlJobPostingDetailsFetcher(client, options),
            "theprotocol.it" => new TheProtocolJobPostingDetailsFetcher(client, options),
            _ => throw new ArgumentOutOfRangeException(nameof(sourceKey))
        };
    }

    private static StubHttpMessageHandler SuccessHandler(string body, string mediaType) =>
        new(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, mediaType)
        });

    private static async Task<string> ReadFixtureAsync(string fileName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "demo-fixtures")))
        {
            directory = directory.Parent;
        }

        var root = directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
        return await File.ReadAllTextAsync(
            Path.Combine(root, "tests", "App.Tests", "Fixtures", "JobPages", fileName));
    }

    private sealed class StubHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            response.RequestMessage = request;
            return Task.FromResult(response);
        }
    }
}
