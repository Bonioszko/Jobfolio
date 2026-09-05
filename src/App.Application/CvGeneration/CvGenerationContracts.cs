using App.Domain;

namespace App.Application;

public sealed record CvWorkflowSettings(
    int MaxGenerationJobsPerWorkspace,
    int MaxCompilationJobsPerWorkspace,
    int MaxUserInstructionCharacters,
    int MaxCustomJobDescriptionCharacters,
    string GeneratorModel);

public sealed record RequestCvGeneration(
    Guid JobPostingId,
    Guid TemplateVersionId,
    Guid CandidateRuleVersionId,
    string? CustomJobDescription,
    string? Instruction);

public enum RequestCvGenerationOutcome
{
    Accepted,
    InvalidInput,
    QuotaExceeded
}

public sealed record RequestCvGenerationResult(
    RequestCvGenerationOutcome Outcome,
    AsyncJobView? Job = null);

public interface ICvGenerationService
{
    Task<RequestCvGenerationResult> RequestAsync(
        string workspaceKey,
        RequestCvGeneration request,
        CancellationToken cancellationToken);

    Task<AsyncJobView?> GetAsync(
        string workspaceKey,
        Guid id,
        CancellationToken cancellationToken);
}

public sealed record AiCvGenerationRequest(
    string SystemRulesMarkdown,
    string JobPostingSnapshotJson,
    string CvTemplateTex,
    string CandidateRulesMarkdown,
    string? UserInstruction);

public sealed record AiCvGenerationResult(string Tex, string Origin);

public interface IAiCvGenerator
{
    Task<AiCvGenerationResult> GenerateAsync(
        AiCvGenerationRequest request,
        CancellationToken cancellationToken);
}

public interface ICvGenerationQueue
{
    Task EnqueueAsync(Guid cvGenerationJobId, CancellationToken cancellationToken);
}

public interface ICvGenerationJobProcessor
{
    Task<bool> ProcessNextAsync(CancellationToken cancellationToken);
}

public sealed record CvGenerationRequestSource(string Title, string NormalizedDataJson);

public sealed record CvGenerationProcessingInputs(
    string CvTemplateTex,
    string CandidateRulesMarkdown);

public interface ICvGenerationStore
{
    Task<CvGenerationRequestSource?> GetRequestSourceAsync(
        string workspaceKey,
        RequestCvGeneration request,
        CancellationToken cancellationToken);

    Task<bool> TryAddAsync(
        CvGenerationJob job,
        int maximumJobsPerWorkspace,
        CancellationToken cancellationToken);
    Task<CvGenerationJob?> GetAsync(
        string workspaceKey,
        Guid id,
        CancellationToken cancellationToken);
    Task<CvGenerationJob?> ClaimNextAsync(
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken);
    Task<CvGenerationProcessingInputs> GetProcessingInputsAsync(
        CvGenerationJob job,
        CancellationToken cancellationToken);
    Task CompleteAsync(
        CvGenerationJob job,
        AiCvGenerationResult result,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken);
    Task SaveAsync(CvGenerationJob job, CancellationToken cancellationToken);
}
