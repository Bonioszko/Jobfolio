using App.Application;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure;

public sealed class CvTemplateQueryService(AppDbContext db) : ICvTemplateQueryService
{
    public async Task<IReadOnlyList<CvTemplateView>> ListAsync(
        string workspaceKey,
        CancellationToken cancellationToken)
    {
        return await (
                from template in db.CvTemplates.AsNoTracking()
                join version in db.CvTemplateVersions.AsNoTracking()
                    on new { TemplateId = template.Id, Version = template.CurrentVersion, template.WorkspaceKey }
                    equals new
                    {
                        TemplateId = version.CvTemplateId,
                        version.Version,
                        version.WorkspaceKey
                    }
                where template.WorkspaceKey == workspaceKey && !template.IsArchived
                orderby template.Name
                select new CvTemplateView(
                    template.Id,
                    template.Name,
                    version.Version,
                    version.Id,
                    version.Tex))
            .ToListAsync(cancellationToken);
    }
}
