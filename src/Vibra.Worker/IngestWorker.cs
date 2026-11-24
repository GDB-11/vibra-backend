namespace Vibra.Worker;

/// <summary>
/// Proceso de ingesta y transcodificación. Por ahora permanece inactivo hasta que se detenga el host.
/// </summary>
public sealed class IngestWorker(ILogger<IngestWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Worker iniciado; todavía no hay trabajos de ingesta.");

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
