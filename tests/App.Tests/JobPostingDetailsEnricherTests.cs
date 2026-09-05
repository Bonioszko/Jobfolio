using App.Application;

namespace App.Tests;

public sealed class JobPostingDetailsEnricherTests
{
    [Fact]
    public async Task Enricher_merges_fetched_details_into_parsed_and_search_data()
    {
        var posting = CreatePosting("linkedin", description: null);
        var enricher = new JobPostingDetailsEnricher([
            new StubFetcher("linkedin", new JobPostingDetails(
                "Full website description", "Remote", "FULL_TIME", "20 000 PLN"))
        ]);

        var enriched = await enricher.EnrichAsync(posting, CancellationToken.None);

        Assert.Equal("Full website description", enriched.ParsedData["description"]);
        Assert.Equal("Remote", enriched.ParsedData["location"]);
        Assert.Equal("FULL_TIME", enriched.SearchData["employmentType"]);
        Assert.Equal("20 000 PLN", enriched.ParsedData["salary"]);
    }

    [Fact]
    public async Task Enricher_keeps_email_description_when_provider_blocks_the_page_request()
    {
        var posting = CreatePosting("indeed", "Description supplied by the Indeed alert.");
        var enricher = new JobPostingDetailsEnricher([
            new StubFetcher("indeed", new HttpRequestException("401 Unauthorized"))
        ]);

        var enriched = await enricher.EnrichAsync(posting, CancellationToken.None);

        Assert.Same(posting, enriched);
        Assert.Equal("Description supplied by the Indeed alert.", enriched.ParsedData["description"]);
    }

    private static ParseResult CreatePosting(string sourceKey, string? description) => new(
        sourceKey,
        "123",
        "Developer",
        new Dictionary<string, object?>
        {
            ["url"] = sourceKey == "indeed"
                ? "https://pl.indeed.com/viewjob?jk=123"
                : "https://www.linkedin.com/jobs/view/123",
            ["description"] = description,
            ["location"] = "Warszawa",
            ["employmentType"] = null,
            ["salary"] = null
        },
        new Dictionary<string, object?>
        {
            ["location"] = "Warszawa",
            ["employmentType"] = null
        });

    private sealed class StubFetcher : IJobPostingDetailsFetcher
    {
        private readonly JobPostingDetails? details;
        private readonly Exception? exception;

        public StubFetcher(string sourceKey, JobPostingDetails details)
        {
            SourceKey = sourceKey;
            this.details = details;
        }

        public StubFetcher(string sourceKey, Exception exception)
        {
            SourceKey = sourceKey;
            this.exception = exception;
        }

        public string SourceKey { get; }

        public Task<JobPostingDetails> FetchAsync(
            JobPostingDetailsReference reference,
            CancellationToken cancellationToken) =>
            exception is null
                ? Task.FromResult(details!)
                : Task.FromException<JobPostingDetails>(exception);
    }
}
