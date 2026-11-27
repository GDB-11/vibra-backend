namespace Vibra.Worker;

/// <summary>
/// Proceso de ingesta y transcodificación. Por ahora permanece inactivo hasta que se detenga el host.
/// </summary>
public sealed class IngestWorker : BackgroundService
{
    private readonly ILogger<IngestWorker> _logger;

    public IngestWorker(ILogger<IngestWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker iniciado; todavía no hay trabajos de ingesta.");

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
