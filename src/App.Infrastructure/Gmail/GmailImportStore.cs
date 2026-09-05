using System.Text.Json;
using App.Application;
using App.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure;

public sealed class GmailImportStore(AppDbContext db) : IGmailImportStore
{
    public Task<bool> IsProcessedAsync(
        string workspaceKey,
        string gmailMessageId,
        CancellationToken cancellationToken) => db.GmailMessageReceipts.AnyAsync(
            receipt => receipt.WorkspaceKey == workspaceKey &&
                       receipt.GmailMessageId == gmailMessageId,
            cancellationToken);

    public async Task SaveAsync(
        string workspaceKey,
        EmailMessage email,
        string processingStatus,
        string? parserKey,
        int? parserVersion,
        IReadOnlyList<ParseResult> postings,
        CancellationToken cancellationToken)
    {
        if (await IsProcessedAsync(workspaceKey, email.ExternalId, cancellationToken)) return;

        foreach (var result in postings)
        {
            var exists = await db.JobPostings.AnyAsync(
                posting => posting.WorkspaceKey == workspaceKey &&
                           posting.ProviderKey == result.SourceKey &&
                           posting.ProviderExternalId == result.SourceExternalId,
                cancellationToken);
            if (exists) continue;

            db.JobPostings.Add(new JobPosting
            {
                WorkspaceKey = workspaceKey,
                ProviderKey = result.SourceKey,
                ProviderExternalId = result.SourceExternalId,
                Title = result.DisplayTitle,
                NormalizedDataJson = JsonSerializer.Serialize(result.ParsedData, JsonDefaults.Web),
                SearchDataJson = JsonSerializer.Serialize(result.SearchData, JsonDefaults.Web),
                ApplicationStatus = "NEW",
                ParserKey = parserKey ?? result.SourceKey,
                ParserVersion = parserVersion ?? 0,
                SourceReceivedAt = email.ReceivedAt
            });
        }

        db.GmailMessageReceipts.Add(new GmailMessageReceipt
        {
            WorkspaceKey = workspaceKey,
            GmailMessageId = email.ExternalId,
            ProcessingStatus = processingStatus,
            ParserKey = parserKey,
            ParserVersion = parserVersion,
            SourceReceivedAt = email.ReceivedAt
        });
        await db.SaveChangesAsync(cancellationToken);
    }
}
