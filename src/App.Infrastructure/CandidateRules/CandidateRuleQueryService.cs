using App.Application;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure;

public sealed class CandidateRuleQueryService(AppDbContext db) : ICandidateRuleQueryService
{
    public async Task<CandidateRuleView> GetCurrentAsync(
        string workspaceKey,
        CancellationToken cancellationToken)
    {
        return await (
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
                    version.Markdown))
            .SingleAsync(cancellationToken);
    }
}
