using App.Application;

namespace App.GmailSync;

public sealed class GmailSyncWorker(
    GmailSyncSettings settings,
    IServiceProvider serviceProvider,
    IServiceScopeFactory scopeFactory,
    IHostApplicationLifetime applicationLifetime,
    ILogger<GmailSyncWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!settings.Enabled)
        {
            logger.LogInformation("Gmail sync is disabled.");
            return;
        }

        logger.LogInformation(
            "Gmail sync started for workspace {WorkspaceKey} with {LabelCount} configured labels.",
            settings.WorkspaceKey,
            settings.Labels.Count);

        if (settings.RunOnce)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch
            {
                Environment.ExitCode = 1;
                throw;
            }
            finally
            {
                applicationLifetime.StopApplication();
            }

            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Gmail synchronization run failed.");
            }

            try
            {
                await Task.Delay(settings.PollInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    internal async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        var emailProvider = serviceProvider.GetRequiredService<IEmailProvider>();
        var messageIds = await emailProvider.ListMessageIdsByLabelsAsync(
            settings.Labels,
            settings.MaxMessagesPerRun,
            cancellationToken);
        var imported = 0;
        var unsupported = 0;
        var ambiguous = 0;
        var alreadyProcessed = 0;
        var failed = 0;

        foreach (var messageId in messageIds)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var importStore = scope.ServiceProvider.GetRequiredService<IGmailImportStore>();
                if (await importStore.IsProcessedAsync(
                        settings.WorkspaceKey,
                        messageId,
                        cancellationToken))
                {
                    alreadyProcessed++;
                    continue;
                }

                var message = await emailProvider.GetMessageAsync(
                    messageId,
                    settings.MaxBodyBytes,
                    cancellationToken);
                var processor = scope.ServiceProvider.GetRequiredService<IGmailMessageProcessor>();
                var outcome = await processor.ProcessAsync(
                    settings.WorkspaceKey,
                    message,
                    cancellationToken);
                switch (outcome)
                {
                    case GmailMessageProcessingOutcome.Imported:
                        imported++;
                        break;
                    case GmailMessageProcessingOutcome.Unsupported:
                        unsupported++;
                        break;
                    case GmailMessageProcessingOutcome.Ambiguous:
                        ambiguous++;
                        break;
                    case GmailMessageProcessingOutcome.AlreadyProcessed:
                        alreadyProcessed++;
                        break;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                failed++;
                logger.LogError(
                    exception,
                    "Gmail message {GmailMessageId} could not be processed.",
                    messageId);
            }
        }

        logger.LogInformation(
            "Gmail sync completed. Scanned={Scanned}, Imported={Imported}, Unsupported={Unsupported}, Ambiguous={Ambiguous}, AlreadyProcessed={AlreadyProcessed}, Failed={Failed}.",
            messageIds.Count,
            imported,
            unsupported,
            ambiguous,
            alreadyProcessed,
            failed);
    }
}
