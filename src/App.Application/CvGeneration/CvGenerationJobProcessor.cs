namespace App.Application;

public sealed class CvGenerationJobProcessor(
    ICvGenerationStore store,
    IAiCvGenerator generator,
    ISystemRulesProvider systemRules,
    ITexSafetyValidator texSafety,
    TimeProvider timeProvider) : ICvGenerationJobProcessor
{
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(2);

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
