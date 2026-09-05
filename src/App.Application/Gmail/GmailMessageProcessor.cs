namespace App.Application;

public sealed class GmailMessageProcessor(
    ISourceParserRegistry parserRegistry,
    IJobPostingDetailsEnricher detailsEnricher,
    IGmailImportStore importStore) : IGmailMessageProcessor
{
    public async Task<GmailMessageProcessingOutcome> ProcessAsync(
        string workspaceKey,
        EmailMessage email,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(workspaceKey))
        {
            throw new ArgumentException("A workspace key is required.", nameof(workspaceKey));
        }

        if (await importStore.IsProcessedAsync(
                workspaceKey,
                email.ExternalId,
                cancellationToken))
        {
            return GmailMessageProcessingOutcome.AlreadyProcessed;
        }

        var selection = parserRegistry.Select(email);
        if (selection.Match == ParserMatch.Unsupported)
        {
            await SaveOutcomeAsync("UNSUPPORTED", null, [], cancellationToken);
            return GmailMessageProcessingOutcome.Unsupported;
        }

        if (selection.Match == ParserMatch.Ambiguous || selection.Parser is null)
        {
            await SaveOutcomeAsync("AMBIGUOUS", null, [], cancellationToken);
            return GmailMessageProcessingOutcome.Ambiguous;
        }

        var parsed = await selection.Parser.ParseAsync(email, cancellationToken);
        var enriched = new List<ParseResult>(parsed.Count);
        foreach (var posting in parsed)
        {
            enriched.Add(await detailsEnricher.EnrichAsync(posting, cancellationToken));
        }

        await SaveOutcomeAsync("IMPORTED", selection.Parser, enriched, cancellationToken);
        return GmailMessageProcessingOutcome.Imported;

        Task SaveOutcomeAsync(
            string status,
            ISourceParser? parser,
            IReadOnlyList<ParseResult> postings,
            CancellationToken token) => importStore.SaveAsync(
                workspaceKey,
                email,
                status,
                parser?.Key,
                parser?.Version,
                postings,
                token);
    }
}
