using App.Application;
using App.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace App.Infrastructure;

public sealed class CvTemplateCommandService(
    AppDbContext db,
    ITexSafetyValidator texSafety) : ICvTemplateCommandService
{
    private const int MaxNameLength = 120;
    private const int MaxTemplatesPerWorkspace = 20;

    public async Task<SaveCvTemplateResult> CreateAsync(
        string workspaceKey,
        SaveCvTemplate request,
        CancellationToken cancellationToken)
    {
        var normalized = Validate(request);
        if (normalized is null)
        {
            return new SaveCvTemplateResult(SaveCvTemplateOutcome.InvalidInput);
        }

        if (await db.CvTemplates.CountAsync(
                template => template.WorkspaceKey == workspaceKey,
                cancellationToken) >= MaxTemplatesPerWorkspace)
        {
            return new SaveCvTemplateResult(SaveCvTemplateOutcome.LimitReached);
        }

        if (await NameExistsAsync(workspaceKey, normalized.Name, null, cancellationToken))
        {
            return new SaveCvTemplateResult(SaveCvTemplateOutcome.Conflict);
        }

        var template = new CvTemplate
        {
            WorkspaceKey = workspaceKey,
            Name = normalized.Name,
            CurrentVersion = 1
        };
        var version = new CvTemplateVersion
        {
            WorkspaceKey = workspaceKey,
            CvTemplateId = template.Id,
            Version = 1,
            Tex = normalized.Tex
        };
        db.AddRange(template, version);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsNameConflict(exception))
        {
            return new SaveCvTemplateResult(SaveCvTemplateOutcome.Conflict);
        }

        return Saved(template, version);
    }

    public async Task<SaveCvTemplateResult> UpdateAsync(
        string workspaceKey,
        Guid templateId,
        SaveCvTemplate request,
        CancellationToken cancellationToken)
    {
        var normalized = Validate(request);
        if (templateId == Guid.Empty || normalized is null)
        {
            return new SaveCvTemplateResult(SaveCvTemplateOutcome.InvalidInput);
        }

        var template = await db.CvTemplates.SingleOrDefaultAsync(
            candidate => candidate.WorkspaceKey == workspaceKey &&
                         candidate.Id == templateId &&
                         !candidate.IsArchived,
            cancellationToken);
        if (template is null)
        {
            return new SaveCvTemplateResult(SaveCvTemplateOutcome.NotFound);
        }

        if (await NameExistsAsync(workspaceKey, normalized.Name, template.Id, cancellationToken))
        {
            return new SaveCvTemplateResult(SaveCvTemplateOutcome.Conflict);
        }

        var version = new CvTemplateVersion
        {
            WorkspaceKey = workspaceKey,
            CvTemplateId = template.Id,
            Version = checked(template.CurrentVersion + 1),
            Tex = normalized.Tex
        };
        template.Name = normalized.Name;
        template.CurrentVersion = version.Version;
        db.CvTemplateVersions.Add(version);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsNameConflict(exception))
        {
            return new SaveCvTemplateResult(SaveCvTemplateOutcome.Conflict);
        }

        return Saved(template, version);
    }

    private SaveCvTemplate? Validate(SaveCvTemplate request)
    {
        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name) ||
            name.Length > MaxNameLength ||
            string.IsNullOrWhiteSpace(request.Tex))
        {
            return null;
        }

        try
        {
            texSafety.Validate(request.Tex);
        }
        catch (InvalidOperationException)
        {
            return null;
        }

        return new SaveCvTemplate(name, request.Tex);
    }

    private Task<bool> NameExistsAsync(
        string workspaceKey,
        string name,
        Guid? excludedId,
        CancellationToken cancellationToken) =>
        db.CvTemplates.AnyAsync(
            template => template.WorkspaceKey == workspaceKey &&
                        template.Name == name &&
                        (!excludedId.HasValue || template.Id != excludedId.Value),
            cancellationToken);

    private static bool IsNameConflict(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "IX_DocumentTemplates_WorkspaceKey_Name"
        };

    private static SaveCvTemplateResult Saved(
        CvTemplate template,
        CvTemplateVersion version) =>
        new(
            SaveCvTemplateOutcome.Saved,
            new CvTemplateView(
                template.Id,
                template.Name,
                version.Version,
                version.Id,
                version.Tex));
}
