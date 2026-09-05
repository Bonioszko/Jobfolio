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

    private static ParseResult CreateParseResult(string? description) => new(
        "linkedin",
        "123",
        "Developer",
        new() { ["url"] = "https://www.linkedin.com/jobs/view/123", ["description"] = description },
        new());

    private sealed class FixedRegistry(ISourceParser? parser) : ISourceParserRegistry
    {
        public ParserSelection Select(EmailMessage email) => parser is null
            ? new(ParserMatch.Unsupported, null)
            : new(ParserMatch.Matched, parser);
    }

    private sealed class StubParser(ParseResult result) : ISourceParser
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
            return Task.FromResult<IReadOnlyList<ParseResult>>([result]);
        }
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
            Postings = postings;
            return Task.CompletedTask;
        }
    }
}
