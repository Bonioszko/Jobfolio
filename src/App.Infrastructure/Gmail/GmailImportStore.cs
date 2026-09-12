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

        var existingPostings = await db.JobPostings
            .AsNoTracking()
            .Where(posting => posting.WorkspaceKey == workspaceKey)
            .Select(posting => new
            {
                posting.ProviderKey,
                posting.ProviderExternalId,
                posting.Title,
                posting.NormalizedDataJson
            })
            .ToListAsync(cancellationToken);
        var knownProviderPostings = existingPostings
            .Where(posting => posting.ProviderExternalId is not null)
            .Select(posting => ProviderPostingKey(
                posting.ProviderKey,
                posting.ProviderExternalId!))
            .ToHashSet(StringComparer.Ordinal);
        var knownPostingIdentities = existingPostings
            .Select(posting => ReadIdentity(posting.Title, posting.NormalizedDataJson))
            .OfType<JobPostingIdentity>()
            .ToList();

        foreach (var result in postings)
        {
            if (!knownProviderPostings.Add(
                    ProviderPostingKey(result.SourceKey, result.SourceExternalId)))
            {
                continue;
            }

            var identity = JobPostingDeduplication.CreateIdentity(result);
            if (identity is not null &&
                knownPostingIdentities.Any(existing =>
                    JobPostingDeduplication.AreDuplicates(existing, identity)))
            {
                continue;
            }

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
            if (identity is not null)
            {
                knownPostingIdentities.Add(identity);
            }
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

    private static string ProviderPostingKey(
        string providerKey,
        string providerExternalId) =>
        string.Concat(providerKey, "\u001f", providerExternalId);

    private static JobPostingIdentity? ReadIdentity(
        string title,
        string normalizedDataJson)
    {
        try
        {
            using var document = JsonDocument.Parse(normalizedDataJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            return JobPostingDeduplication.CreateIdentity(
                title,
                ReadString(document.RootElement, "company"),
                ReadString(document.RootElement, "location"));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ReadString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) &&
        property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
}
