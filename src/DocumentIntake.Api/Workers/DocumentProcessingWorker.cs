using DocumentIntake.Api.Abstractions;

namespace DocumentIntake.Api.Workers;

public sealed class DocumentProcessingWorker(IProcessingQueue queue, IDocumentProcessor processor, ILogger<DocumentProcessingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var message = await queue.DequeueAsync(stoppingToken);
                await processor.ProcessAsync(message, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled worker error.");
            }
        }
    }
}
