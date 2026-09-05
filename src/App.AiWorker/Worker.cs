using App.Application;

namespace App.AiWorker;

public sealed class Worker(
    IServiceScopeFactory scopeFactory,
    ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var processor = scope.ServiceProvider
                    .GetRequiredService<ICvGenerationJobProcessor>();
                var processed = await processor.ProcessNextAsync(stoppingToken);
                if (!processed) await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "CV generation worker iteration failed.");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }
}
