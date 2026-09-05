namespace App.Application;

public sealed class CvCompilationJobProcessor(
    ICvCompilationStore store,
    ITexCompiler compiler,
    IArtifactStorage storage,
    TimeProvider timeProvider) : ICvCompilationJobProcessor
{
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(1);

    public async Task<bool> ProcessNextAsync(CancellationToken cancellationToken)
    {
        var job = await store.ClaimNextAsync(
            timeProvider.GetUtcNow(),
            LeaseDuration,
            cancellationToken);
        if (job is null) return false;

        try
        {
            var tex = await store.GetTexAsync(job, cancellationToken);
            var pdf = await compiler.CompileAsync(tex, cancellationToken);
            var artifactId = job.Id;
            var stored = await storage.SaveAsync(
                job.WorkspaceKey,
                artifactId,
                pdf,
                cancellationToken);
            await store.CompleteAsync(
                job,
                stored,
                timeProvider.GetUtcNow(),
                cancellationToken);
            return true;
        }
        catch (TexCompilationTimeoutException exception)
        {
            job.MarkTimedOut(timeProvider.GetUtcNow(), exception.Message);
            await store.SaveAsync(job, cancellationToken);
            return true;
        }
        catch (Exception exception) when (exception is InvalidOperationException or FormatException)
        {
            job.MarkFailed(timeProvider.GetUtcNow(), exception.Message);
            await store.SaveAsync(job, cancellationToken);
            return true;
        }
    }
}
