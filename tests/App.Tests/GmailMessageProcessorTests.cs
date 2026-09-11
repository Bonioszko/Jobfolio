using App.Application;

namespace App.Tests;

public sealed class GmailMessageProcessorTests
{
    [Fact]
    public async Task Processor_parses_enriches_and_saves_a_supported_message()
    {
        var parsed = CreateParseResult(description: null);
        var store = new RecordingStore();
        var processor = new GmailMessageProcessor(
            new FixedRegistry(new StubParser(parsed)),
            new StubEnricher("Full website description"),
            store);

        var outcome = await processor.ProcessAsync(
            "user:owner",
            CreateEmail(),
            CancellationToken.None);

        Assert.Equal(GmailMessageProcessingOutcome.Imported, outcome);
        Assert.Equal("IMPORTED", store.Status);
        var saved = Assert.Single(store.Postings);
        Assert.Equal("Full website description", saved.ParsedData["description"]);
    }

    [Fact]
    public async Task Processor_saves_every_posting_when_one_page_cannot_be_enriched()
    {
        var enrichable = CreateParseResult("enrichable", description: null);
        var unavailable = CreateParseResult("unavailable", description: null);
        var store = new RecordingStore();
        var enricher = new JobPostingDetailsEnricher([
            new SelectiveFetcher("linkedin", "enrichable", "Full website description")
        ]);
        var processor = new GmailMessageProcessor(
            new FixedRegistry(new StubParser(enrichable, unavailable)),
            enricher,
            store);

        var outcome = await processor.ProcessAsync(
            "user:owner",
            CreateEmail(),
            CancellationToken.None);

        Assert.Equal(GmailMessageProcessingOutcome.Imported, outcome);
        Assert.Equal("IMPORTED", store.Status);
        Assert.Equal(2, store.Postings.Count);
        Assert.Equal("Full website description", store.Postings[0].ParsedData["description"]);
        Assert.Null(store.Postings[1].ParsedData["description"]);
    }

    [Fact]
    public async Task Processor_records_unsupported_messages_without_persisting_raw_content()
    {
        var store = new RecordingStore();
        var processor = new GmailMessageProcessor(
            new FixedRegistry(null),
            new StubEnricher("unused"),
            store);

        var outcome = await processor.ProcessAsync(
            "user:owner",
            CreateEmail(),
            CancellationToken.None);

        Assert.Equal(GmailMessageProcessingOutcome.Unsupported, outcome);
        Assert.Equal("UNSUPPORTED", store.Status);
        Assert.Empty(store.Postings);
    }

    [Fact]
    public async Task Processor_records_a_matched_message_with_an_unrecognized_layout_as_unsupported()
    {
        var store = new RecordingStore();
        var processor = new GmailMessageProcessor(
            new FixedRegistry(new UnrecognizedMessageParser()),
            new StubEnricher("unused"),
            store);

        var outcome = await processor.ProcessAsync(
            "user:owner",
            CreateEmail(),
            CancellationToken.None);

        Assert.Equal(GmailMessageProcessingOutcome.Unsupported, outcome);
        Assert.Equal("UNSUPPORTED", store.Status);
        Assert.Equal("indeed", store.ParserKey);
        Assert.Equal(3, store.ParserVersion);
        Assert.Empty(store.Postings);
    }

    [Fact]
    public async Task Processor_skips_a_message_receipt_that_already_exists()
    {
        var store = new RecordingStore { AlreadyProcessed = true };
        var parser = new StubParser(CreateParseResult(null));
        var processor = new GmailMessageProcessor(
            new FixedRegistry(parser),
            new StubEnricher("unused"),
            store);

        var outcome = await processor.ProcessAsync(
            "user:owner",
            CreateEmail(),
            CancellationToken.None);

        Assert.Equal(GmailMessageProcessingOutcome.AlreadyProcessed, outcome);
        Assert.Equal(0, parser.ParseCalls);
    }

    private static EmailMessage CreateEmail() => new(
        "gmail-id",
        "jobs@linkedin.com",
        "Jobs",
        "sensitive email body",
        DateTimeOffset.UtcNow);

    private static ParseResult CreateParseResult(string? description) =>
        CreateParseResult("123", description);

    private static ParseResult CreateParseResult(string externalId, string? description) => new(
        "linkedin",
        externalId,
        "Developer",
        new()
        {
            ["url"] = $"https://www.linkedin.com/jobs/view/{externalId}",
            ["description"] = description
        },
        new());

    private sealed class FixedRegistry(ISourceParser? parser) : ISourceParserRegistry
    {
        public ParserSelection Select(EmailMessage email) => parser is null
            ? new(ParserMatch.Unsupported, null)
            : new(ParserMatch.Matched, parser);
    }

    private sealed class StubParser(params ParseResult[] results) : ISourceParser
    {
        public int ParseCalls { get; private set; }
        public string Key => "linkedin";
        public int Version => 2;
        public bool CanParse(EmailMessage email) => true;

        public Task<IReadOnlyList<ParseResult>> ParseAsync(
            EmailMessage email,
            CancellationToken cancellationToken)
        {
            ParseCalls++;
            return Task.FromResult<IReadOnlyList<ParseResult>>(results);
        }
    }

    private sealed class UnrecognizedMessageParser : ISourceParser
    {
        public string Key => "indeed";
        public int Version => 3;
        public bool CanParse(EmailMessage email) => true;

        public Task<IReadOnlyList<ParseResult>> ParseAsync(
            EmailMessage email,
            CancellationToken cancellationToken) =>
            Task.FromException<IReadOnlyList<ParseResult>>(
                new FormatException("The email did not contain a recognizable job posting."));
    }

    private sealed class SelectiveFetcher(
        string sourceKey,
        string enrichableExternalId,
        string description) : IJobPostingDetailsFetcher
    {
        public string SourceKey => sourceKey;

        public Task<JobPostingDetails> FetchAsync(
            JobPostingDetailsReference reference,
            CancellationToken cancellationToken) =>
            reference.SourceExternalId == enrichableExternalId
                ? Task.FromResult(new JobPostingDetails(description, null, null, null))
                : Task.FromException<JobPostingDetails>(
                    new FormatException("The provider page does not contain job details."));
    }

    private sealed class StubEnricher(string description) : IJobPostingDetailsEnricher
    {
        public Task<ParseResult> EnrichAsync(ParseResult posting, CancellationToken cancellationToken)
        {
            var data = new Dictionary<string, object?>(posting.ParsedData) { ["description"] = description };
            return Task.FromResult(posting with { ParsedData = data });
        }
    }

    private sealed class RecordingStore : IGmailImportStore
    {
        public bool AlreadyProcessed { get; init; }
        public string? Status { get; private set; }
        public string? ParserKey { get; private set; }
        public int? ParserVersion { get; private set; }
        public IReadOnlyList<ParseResult> Postings { get; private set; } = [];

        public Task<bool> IsProcessedAsync(
            string workspaceKey,
            string gmailMessageId,
            CancellationToken cancellationToken) => Task.FromResult(AlreadyProcessed);

        public Task SaveAsync(
            string workspaceKey,
            EmailMessage email,
            string processingStatus,
            string? parserKey,
            int? parserVersion,
            IReadOnlyList<ParseResult> postings,
            CancellationToken cancellationToken)
        {
            Status = processingStatus;
            ParserKey = parserKey;
            ParserVersion = parserVersion;
            Postings = postings;
            return Task.CompletedTask;
        }
    }
}
