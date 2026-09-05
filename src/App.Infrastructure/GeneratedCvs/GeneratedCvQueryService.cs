using App.Application;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure;

public sealed class GeneratedCvQueryService(AppDbContext db) : IGeneratedCvQueryService
{
    public async Task<GeneratedCvView?> GetAsync(
        string workspaceKey,
        Guid id,
        CancellationToken cancellationToken)
    {
        return await (
                from document in db.GeneratedCvs.AsNoTracking()
                join version in db.GeneratedCvVersions.AsNoTracking()
                    on new
                    {
                        DocumentId = document.Id,
                        Version = document.CurrentVersion,
                        document.WorkspaceKey
                    }
                    equals new
                    {
                        DocumentId = version.GeneratedCvId,
                        version.Version,
                        version.WorkspaceKey
                    }
                where document.WorkspaceKey == workspaceKey && document.Id == id
                select new GeneratedCvView(
                    document.Id,
                    version.Version,
                    version.Id,
                    version.Tex,
                    version.Origin))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
