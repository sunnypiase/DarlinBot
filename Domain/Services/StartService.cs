namespace Darlin.Domain.Services;

public class StartService
{
    private readonly ILogger<StartService> _logger;
    private readonly TickerManager _tm;

    public StartService(
        TickerManager tm,
        ILogger<StartService> logger)
    {
        _tm = tm;
        _logger = logger;
    }

    public Task RunAsync(CancellationToken ct)
    {
        _logger.LogInformation(
            "Starting event loops for {Count} tickers…",
            _tm.Tickers.Count);

        foreach (var t in _tm.Tickers)
            _ = Task.Run(async () =>
            {
                _logger.LogInformation("→ {Name} loop start", t.Name);
                await t.Start();
            }, ct);

        return Task.CompletedTask;
    }
}