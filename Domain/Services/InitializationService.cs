using System.Collections.Concurrent;

namespace Darlin.Domain.Services;

public class InitializationService
{
    private readonly TickerManager                  _tm;
    private readonly ILogger<InitializationService> _logger;
    private const int BatchSize = 50;
    private static readonly TimeSpan DelayBetweenBatches =
        TimeSpan.FromMinutes(1.25);

    public InitializationService(
        TickerManager tm,
        ILogger<InitializationService> logger)
    {
        _tm     = tm;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        var tickers = _tm.Tickers;
        _logger.LogInformation(
            "Initializing {Count} tickers in batches of {BatchSize}…",
            tickers.Count, BatchSize);

        var success = new ConcurrentBag<string>();
        var failure = new ConcurrentBag<string>();
        int idx = 0;

        for (int i = 0; i < tickers.Count; i += BatchSize)
        {
            var batch = tickers.Skip(i).Take(BatchSize);
            var tasks = batch.Select(t =>
                Task.Run(async () =>
                {
                    int myId = Interlocked.Increment(ref idx);
                    for (int attempt = 1; attempt <= 5; attempt++)
                    {
                        _logger.LogInformation(
                            "Ticker #{Id} init {Name} ({Attempt}/5)…",
                            myId, t.Name, attempt);

                        if (await t.Initialize())
                        {
                            success.Add(t.Name);
                            _logger.LogInformation(
                                "Ticker #{Id} initialized → starting loop",
                                myId);

                            // ⚡ Immediately kick off its event-loop
                            _ = Task.Run(async () =>
                            {
                                _logger.LogInformation(
                                    "Ticker #{Id} loop started",
                                    myId);
                                await t.Start();
                            }, ct);

                            return; // done with this ticker
                        }

                        await Task.Delay(TimeSpan.FromSeconds(5), ct);
                    }

                    failure.Add(t.Name);
                }, ct)).ToArray();

            await Task.WhenAll(tasks);

            _logger.LogInformation(
                "Batch {BatchNo} done. Waiting {Delay}…",
                (i / BatchSize) + 1,
                DelayBetweenBatches);
            await Task.Delay(DelayBetweenBatches, ct);
        }

        _logger.LogInformation(
            "Initialization complete: {OK}/{Total}",
            success.Count, tickers.Count);
        if (failure.Any())
            _logger.LogWarning(
                "Failed to initialize: {Failed}",
                string.Join(", ", failure));
    }
}
