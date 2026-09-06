using App.Domain;
using App.Application;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace App.Infrastructure;

public sealed class CvTemplateQueryService(AppDbContext db) : ICvTemplateQueryService
{
    public async Task<IReadOnlyList<CvTemplateView>> ListAsync(
        string workspaceKey,
        CancellationToken cancellationToken)
    {
        var templates = await ListQuery(workspaceKey).ToListAsync(cancellationToken);
        if (templates.Count > 0)
        {
            return templates;
        }

        var templateRoot = RepositoryFileLocator.FindDirectory("example-templates");
        foreach (var file in Directory.EnumerateFiles(templateRoot, "*.tex").Order())
        {
            var template = new CvTemplate
            {
                WorkspaceKey = workspaceKey,
                Name = Path.GetFileNameWithoutExtension(file),
                CurrentVersion = 1
            };
            db.CvTemplates.Add(template);
            db.CvTemplateVersions.Add(new CvTemplateVersion
            {
                WorkspaceKey = workspaceKey,
                CvTemplateId = template.Id,
                Version = 1,
                Tex = await File.ReadAllTextAsync(file, cancellationToken)
            });
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "IX_DocumentTemplates_WorkspaceKey_Name"
            })
        {
            db.ChangeTracker.Clear();
        }

        return await ListQuery(workspaceKey).ToListAsync(cancellationToken);
    }

    private IQueryable<CvTemplateView> ListQuery(string workspaceKey) =>
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
            version.Tex);
}
