using App.Domain;

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

        await ProcessClaimedAsync(job, cancellationToken);
        return true;
    }

    public async Task<CvCompilationProcessingOutcome> ProcessAsync(
        Guid cvCompileJobId,
        CancellationToken cancellationToken)
    {
        if (cvCompileJobId == Guid.Empty)
        {
            return CvCompilationProcessingOutcome.NotFound;
        }

        var claim = await store.ClaimAsync(
            cvCompileJobId,
            timeProvider.GetUtcNow(),
            LeaseDuration,
            cancellationToken);

        if (claim.Outcome != CvCompilationClaimOutcome.Claimed)
        {
            return claim.Outcome switch
            {
                CvCompilationClaimOutcome.AlreadyTerminal =>
                    CvCompilationProcessingOutcome.AlreadyTerminal,
                CvCompilationClaimOutcome.Deferred =>
                    CvCompilationProcessingOutcome.Deferred,
                CvCompilationClaimOutcome.NotFound =>
                    CvCompilationProcessingOutcome.NotFound,
                _ => throw new InvalidOperationException(
                    $"Unsupported compilation claim outcome '{claim.Outcome}'.")
            };
        }

        await ProcessClaimedAsync(
            claim.Job ?? throw new InvalidOperationException("A claimed compilation has no job."),
            cancellationToken);
        return CvCompilationProcessingOutcome.Processed;
    }

    private async Task ProcessClaimedAsync(
        CvCompileJob job,
        CancellationToken cancellationToken)
    {
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
        }
        catch (TexCompilationTimeoutException exception)
        {
            job.MarkTimedOut(timeProvider.GetUtcNow(), exception.Message);
            await store.SaveAsync(job, cancellationToken);
        }
        catch (Exception exception) when (exception is InvalidOperationException or FormatException)
        {
            job.MarkFailed(timeProvider.GetUtcNow(), exception.Message);
            await store.SaveAsync(job, cancellationToken);
        }
    }
}
