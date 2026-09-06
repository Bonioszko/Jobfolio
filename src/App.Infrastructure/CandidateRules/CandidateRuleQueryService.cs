using App.Application;
using App.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace App.Infrastructure;

public sealed class CandidateRuleQueryService(AppDbContext db) : ICandidateRuleQueryService
{
    private const string InitialMarkdown = """
        # Candidate facts

        No verified candidate facts have been added yet.
        """;

    public async Task<CandidateRuleView> GetCurrentAsync(
        string workspaceKey,
        CancellationToken cancellationToken)
    {
        var current = await GetCurrentQuery(workspaceKey)
            .SingleOrDefaultAsync(cancellationToken);

        if (current is not null)
        {
            return current;
        }

        var documentExists = await db.CandidateRuleDocuments
            .AnyAsync(document => document.WorkspaceKey == workspaceKey, cancellationToken);
        if (documentExists)
        {
            return await GetCurrentOrThrowAsync(workspaceKey, cancellationToken);
        }

        var newDocument = new CandidateRuleDocument
        {
            WorkspaceKey = workspaceKey,
            CurrentVersion = 1
        };
        var newVersion = new CandidateRuleVersion
        {
            WorkspaceKey = workspaceKey,
            CandidateRuleDocumentId = newDocument.Id,
            Version = 1,
            Markdown = InitialMarkdown
        };

        db.CandidateRuleDocuments.Add(newDocument);
        db.CandidateRuleVersions.Add(newVersion);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "IX_UserRuleDocuments_WorkspaceKey"
            })
        {
            db.Entry(newVersion).State = EntityState.Detached;
            db.Entry(newDocument).State = EntityState.Detached;
            return await GetCurrentOrThrowAsync(workspaceKey, cancellationToken);
        }

        return new CandidateRuleView(
            newDocument.Id,
            newVersion.Version,
            newVersion.Id,
            newVersion.Markdown);
    }

    private IQueryable<CandidateRuleView> GetCurrentQuery(string workspaceKey) =>
        from document in db.CandidateRuleDocuments.AsNoTracking()
        join version in db.CandidateRuleVersions.AsNoTracking()
            on new
            {
                DocumentId = document.Id,
                Version = document.CurrentVersion,
                document.WorkspaceKey
            }
            equals new
            {
                DocumentId = version.CandidateRuleDocumentId,
                version.Version,
                version.WorkspaceKey
            }
        where document.WorkspaceKey == workspaceKey
        select new CandidateRuleView(
            document.Id,
            version.Version,
            version.Id,
            version.Markdown);

    private async Task<CandidateRuleView> GetCurrentOrThrowAsync(
        string workspaceKey,
        CancellationToken cancellationToken)
    {
        var current = await GetCurrentQuery(workspaceKey)
            .SingleOrDefaultAsync(cancellationToken);
        return current ?? throw new InvalidOperationException(
            $"Candidate rules for workspace '{workspaceKey}' do not reference an existing current version.");
    }
}
