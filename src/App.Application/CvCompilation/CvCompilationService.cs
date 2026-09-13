using App.Domain;

namespace App.Application;

public sealed class CvCompilationService(
    ICvCompilationStore store,
    ICvCompilationQueue queue,
    CvWorkflowSettings settings,
    DemoSettings demoSettings,
    TimeProvider timeProvider) : ICvCompilationService
{
    public async Task<RequestCvCompilationResult> RequestAsync(
        string workspaceKey,
        UserMode userMode,
        Guid generatedCvVersionId,
        CancellationToken cancellationToken)
    {
        if (generatedCvVersionId == Guid.Empty ||
            !await store.VersionExistsAsync(workspaceKey, generatedCvVersionId, cancellationToken))
        {
            return new RequestCvCompilationResult(RequestCvCompilationOutcome.InvalidInput);
        }

        var now = timeProvider.GetUtcNow();
        var job = new CvCompileJob
        {
            WorkspaceKey = workspaceKey,
            GeneratedCvVersionId = generatedCvVersionId,
            CreatedAt = now
        };
        var added = await store.TryAddAsync(
            job,
            settings.MaxCompilationJobsPerWorkspace,
            CreateDemoQuota(userMode, now),
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
        UserMode userMode,
        Guid templateVersionId,
        CancellationToken cancellationToken)
    {
        if (templateVersionId == Guid.Empty ||
            !await store.TemplateVersionExistsAsync(workspaceKey, templateVersionId, cancellationToken))
        {
            return new RequestCvCompilationResult(RequestCvCompilationOutcome.InvalidInput);
        }

        var now = timeProvider.GetUtcNow();
        var job = new CvCompileJob
        {
            WorkspaceKey = workspaceKey,
            CvTemplateVersionId = templateVersionId,
            CreatedAt = now
        };
        var added = await store.TryAddAsync(
            job,
            settings.MaxCompilationJobsPerWorkspace,
            CreateDemoQuota(userMode, now),
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

    private DemoCompilationQuota? CreateDemoQuota(UserMode userMode, DateTimeOffset now) =>
        userMode == UserMode.Demo
            ? new DemoCompilationQuota(
                now.Subtract(demoSettings.CompilationWindow),
                demoSettings.MaxCompilationJobsPerWindow)
            : null;
}
