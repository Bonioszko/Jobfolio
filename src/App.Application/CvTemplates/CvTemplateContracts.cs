namespace App.Application;

public sealed record CvTemplateView(Guid Id, string Name, int Version, Guid VersionId, string Tex);

public sealed record SaveCvTemplate(string Name, string Tex);

public enum SaveCvTemplateOutcome
{
    Saved,
    InvalidInput,
    NotFound,
    Conflict,
    LimitReached
}

public sealed record SaveCvTemplateResult(
    SaveCvTemplateOutcome Outcome,
    CvTemplateView? Template = null);

public interface ICvTemplateQueryService
{
    Task<IReadOnlyList<CvTemplateView>> ListAsync(
        string workspaceKey,
        CancellationToken cancellationToken);
}

public interface ICvTemplateCommandService
{
    Task<SaveCvTemplateResult> CreateAsync(
        string workspaceKey,
        SaveCvTemplate request,
        CancellationToken cancellationToken);

    Task<SaveCvTemplateResult> UpdateAsync(
        string workspaceKey,
        Guid templateId,
        SaveCvTemplate request,
        CancellationToken cancellationToken);
}
