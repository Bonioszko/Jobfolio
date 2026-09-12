using App.Domain;

namespace App.Application;

public enum RequestCvCompilationOutcome
{
    Accepted,
    InvalidInput,
    QuotaExceeded
}

public sealed record RequestCvCompilationResult(
    RequestCvCompilationOutcome Outcome,
    AsyncJobView? Job = null);

public interface ICvCompilationService
{
    Task<RequestCvCompilationResult> RequestAsync(
        string workspaceKey,
        Guid generatedCvVersionId,
        CancellationToken cancellationToken);

    Task<RequestCvCompilationResult> RequestTemplateAsync(
        string workspaceKey,
        Guid templateVersionId,
        CancellationToken cancellationToken);

    Task<AsyncJobView?> GetAsync(
        string workspaceKey,
        Guid id,
        CancellationToken cancellationToken);
}

public interface ICvCompilationQueue
{
    Task EnqueueAsync(Guid cvCompileJobId, CancellationToken cancellationToken);
}

public interface ICvCompilationJobProcessor
{
    Task<bool> ProcessNextAsync(CancellationToken cancellationToken);
}

public interface ITexCompiler
{
    Task<byte[]> CompileAsync(string tex, CancellationToken cancellationToken);
}

public interface ITexSafetyValidator
{
    void Validate(string tex);
}

public sealed record StoredArtifact(
    Guid Id,
    string Key,
    string Sha256,
    long Size);

public interface IArtifactStorage
{
    Task<StoredArtifact> SaveAsync(
        string workspaceKey,
        Guid artifactId,
        byte[] data,
        CancellationToken cancellationToken);

    Task<Stream?> OpenReadAsync(
        string key,
        CancellationToken cancellationToken);

    Task DeleteWorkspaceAsync(
        string workspaceKey,
        CancellationToken cancellationToken);
}

public interface IPdfArtifactService
{
    Task<Stream?> OpenReadAsync(
        string workspaceKey,
        Guid artifactId,
        CancellationToken cancellationToken);
}

public interface ICvCompilationStore
{
    Task<bool> VersionExistsAsync(
        string workspaceKey,
        Guid generatedCvVersionId,
        CancellationToken cancellationToken);
    Task<bool> TemplateVersionExistsAsync(
        string workspaceKey,
        Guid templateVersionId,
        CancellationToken cancellationToken);
    Task<bool> TryAddAsync(
        CvCompileJob job,
        int maximumJobsPerWorkspace,
        CancellationToken cancellationToken);
    Task<CvCompileJob?> GetAsync(
        string workspaceKey,
        Guid id,
        CancellationToken cancellationToken);
    Task<CvCompileJob?> ClaimNextAsync(
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken);
    Task<string> GetTexAsync(CvCompileJob job, CancellationToken cancellationToken);
    Task CompleteAsync(
        CvCompileJob job,
        StoredArtifact storedArtifact,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken);
    Task SaveAsync(CvCompileJob job, CancellationToken cancellationToken);
}

public sealed class TexCompilationTimeoutException(string message, Exception? innerException = null)
    : Exception(message, innerException);
