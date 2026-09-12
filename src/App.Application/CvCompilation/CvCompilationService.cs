using App.Domain;

namespace App.Application;

public sealed class CvCompilationService(
    ICvCompilationStore store,
    ICvCompilationQueue queue,
    CvWorkflowSettings settings) : ICvCompilationService
{
    public async Task<RequestCvCompilationResult> RequestAsync(
        string workspaceKey,
        Guid generatedCvVersionId,
        CancellationToken cancellationToken)
    {
        if (generatedCvVersionId == Guid.Empty ||
            !await store.VersionExistsAsync(workspaceKey, generatedCvVersionId, cancellationToken))
        {
            return new RequestCvCompilationResult(RequestCvCompilationOutcome.InvalidInput);
        }

        var job = new CvCompileJob
        {
            WorkspaceKey = workspaceKey,
            GeneratedCvVersionId = generatedCvVersionId
        };
        var added = await store.TryAddAsync(
            job,
            settings.MaxCompilationJobsPerWorkspace,
            cancellationToken);
        if (!added)
        {
            return new RequestCvCompilationResult(RequestCvCompilationOutcome.QuotaExceeded);
        }

        await queue.EnqueueAsync(job.Id, cancellationToken);

        return new RequestCvCompilationResult(
            RequestCvCompilationOutcome.Accepted,
            Map(job));
    }

    public async Task<RequestCvCompilationResult> RequestTemplateAsync(
        string workspaceKey,
        Guid templateVersionId,
        CancellationToken cancellationToken)
    {
        if (templateVersionId == Guid.Empty ||
            !await store.TemplateVersionExistsAsync(workspaceKey, templateVersionId, cancellationToken))
        {
            return new RequestCvCompilationResult(RequestCvCompilationOutcome.InvalidInput);
        }

        var job = new CvCompileJob
        {
            WorkspaceKey = workspaceKey,
            CvTemplateVersionId = templateVersionId
        };
        var added = await store.TryAddAsync(
            job,
            settings.MaxCompilationJobsPerWorkspace,
            cancellationToken);
        if (!added)
        {
            return new RequestCvCompilationResult(RequestCvCompilationOutcome.QuotaExceeded);
        }

        await queue.EnqueueAsync(job.Id, cancellationToken);
        return new RequestCvCompilationResult(RequestCvCompilationOutcome.Accepted, Map(job));
    }

    public async Task<AsyncJobView?> GetAsync(
        string workspaceKey,
        Guid id,
        CancellationToken cancellationToken)
    {
        var job = await store.GetAsync(workspaceKey, id, cancellationToken);
        return job is null ? null : Map(job);
    }

    private static AsyncJobView Map(CvCompileJob job) =>
        new(job.Id, job.Status, job.Error, job.PdfArtifactId);
}
