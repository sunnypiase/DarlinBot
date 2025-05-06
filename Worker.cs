using Darlin.Domain.Services;

namespace Darlin;

public class Worker : BackgroundService
{
    private readonly PreInitializationService _preInit;
    private readonly InitializationService    _init;
    private readonly ILogger<Worker>          _logger;

    public Worker(
        PreInitializationService preInit,
        InitializationService init,
        ILogger<Worker> logger)
    {
        _preInit = preInit;
        _init    = init;
        _logger  = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("▶ Pre-initialization…");
        await _preInit.RunAsync(stoppingToken);

        _logger.LogInformation("Waiting 5 s for streams to connect…");
        await Task.Delay(TimeSpan.FromSeconds(50), stoppingToken);

        _logger.LogInformation("▶ Initializing (and starting) tickers…");
        await _init.RunAsync(stoppingToken);

        _logger.LogInformation("▶ All tickers are running. Waiting until cancellation.");
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}