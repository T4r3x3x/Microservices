namespace Notifications.Worker;

public partial class Worker(ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogWorkerStarted(logger);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    [LoggerMessage(LogLevel.Information, "Notifications worker started")]
    private static partial void LogWorkerStarted(ILogger logger);
}
