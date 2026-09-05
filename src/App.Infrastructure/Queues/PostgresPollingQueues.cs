using App.Application;

namespace App.Infrastructure;

public sealed class PostgresCvGenerationQueue : ICvGenerationQueue
{
    public Task EnqueueAsync(Guid cvGenerationJobId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}

public sealed class PostgresCvCompilationQueue : ICvCompilationQueue
{
    public Task EnqueueAsync(Guid cvCompileJobId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}
