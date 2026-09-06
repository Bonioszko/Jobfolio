using System.Text.Json;
using App.Domain;

namespace App.Application;

public sealed class CvGenerationService(
    ICvGenerationStore store,
    ICvGenerationQueue queue,
    ISystemRulesProvider systemRules,
    CvWorkflowSettings settings) : ICvGenerationService
{
    public async Task<RequestCvGenerationResult> RequestAsync(
        string workspaceKey,
        UserMode userMode,
        RequestCvGeneration request,
        CancellationToken cancellationToken)
    {
        if (request.JobPostingId == Guid.Empty ||
            request.TemplateVersionId == Guid.Empty ||
            request.CandidateRuleVersionId == Guid.Empty ||
            request.CustomJobDescription?.Length > settings.MaxCustomJobDescriptionCharacters ||
            request.Instruction?.Length > settings.MaxUserInstructionCharacters)
        {
            return new RequestCvGenerationResult(RequestCvGenerationOutcome.InvalidInput);
        }

        var source = await store.GetRequestSourceAsync(
            workspaceKey,
            request,
            cancellationToken);
        if (source is null)
        {
            return new RequestCvGenerationResult(RequestCvGenerationOutcome.InvalidInput);
        }

        var snapshot = JsonSerializer.Serialize(
            new
            {
                displayTitle = source.Title,
                parsedData = JsonSerializer.Deserialize<object>(
                    source.NormalizedDataJson,
                    JsonDefaults.Web),
                customJobDescription = NormalizeOptionalText(request.CustomJobDescription)
            },
            JsonDefaults.Web);
        var job = new CvGenerationJob
        {
            WorkspaceKey = workspaceKey,
            JobPostingId = request.JobPostingId,
            CvTemplateVersionId = request.TemplateVersionId,
            CandidateRuleVersionId = request.CandidateRuleVersionId,
            JobPostingSnapshotJson = snapshot,
            SystemRulesHash = systemRules.GenerationRulesHash,
            Model = userMode == UserMode.Demo
                ? CvGeneratorNames.Demo
                : settings.RealUserGenerator,
            UserInstruction = request.Instruction
        };

        var added = await store.TryAddAsync(
            job,
            settings.MaxGenerationJobsPerWorkspace,
            cancellationToken);
        if (!added)
        {
            return new RequestCvGenerationResult(RequestCvGenerationOutcome.QuotaExceeded);
        }

        await queue.EnqueueAsync(job.Id, cancellationToken);

        return new RequestCvGenerationResult(
            RequestCvGenerationOutcome.Accepted,
            Map(job));
    }

    public async Task<AsyncJobView?> GetAsync(
        string workspaceKey,
        Guid id,
        CancellationToken cancellationToken)
    {
        var job = await store.GetAsync(workspaceKey, id, cancellationToken);
        return job is null ? null : Map(job);
    }

    private static AsyncJobView Map(CvGenerationJob job) =>
        new(job.Id, job.Status, job.Error, job.GeneratedCvId);

    private static string? NormalizeOptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
