namespace App.AiWorker;

public class Worker(IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = scopeFactory.CreateScope();
            var processed = await scope.ServiceProvider.GetRequiredService<App.Infrastructure.JobProcessor>().ProcessNextGenerationAsync(stoppingToken);
            if (!processed) await Task.Delay(1000, stoppingToken);
        }
    }
}
