namespace App.Application;

public sealed class CvGenerationJobProcessor(
    ICvGenerationStore store,
    IAiCvGeneratorResolver generatorResolver,
    ISystemRulesProvider systemRules,
    ITexSafetyValidator texSafety,
    TimeProvider timeProvider) : ICvGenerationJobProcessor
{
    // Keep the lease longer than the local Codex timeout so another worker cannot
    // reclaim the same generation while the CLI process is still active.
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(10);

    public async Task<bool> ProcessNextAsync(CancellationToken cancellationToken)
    {
        var job = await store.ClaimNextAsync(
            timeProvider.GetUtcNow(),
            LeaseDuration,
            cancellationToken);
        if (job is null) return false;

        try
        {
            if (!string.Equals(
                    job.SystemRulesHash,
                    systemRules.GenerationRulesHash,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "System generation rules changed after this job was requested.");
            }

            var inputs = await store.GetProcessingInputsAsync(job, cancellationToken);
            var generator = generatorResolver.Resolve(job.Model);
            var result = await generator.GenerateAsync(
                new AiCvGenerationRequest(
                    systemRules.GenerationRulesMarkdown,
                    job.JobPostingSnapshotJson,
                    inputs.CvTemplateTex,
                    inputs.CandidateRulesMarkdown,
                    job.UserInstruction),
                cancellationToken);
            texSafety.Validate(result.Tex);
            await store.CompleteAsync(
                job,
                result,
                timeProvider.GetUtcNow(),
                cancellationToken);
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
