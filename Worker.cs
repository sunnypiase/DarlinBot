using Darlin.Domain.Services;

namespace Darlin;

public class Worker(
    PreInitializationService preInit,
    InitializationService init,
    ILogger<Worker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("▶ Pre-initialization…");
        await preInit.RunAsync(stoppingToken);

        logger.LogInformation("Waiting 50 s for streams to connect…");
        await Task.Delay(TimeSpan.FromSeconds(50), stoppingToken);

        logger.LogInformation("▶ Initializing (and starting) tickers…");
        await init.RunAsync(stoppingToken);

        logger.LogInformation("▶ All tickers are running. Waiting until cancellation.");
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}