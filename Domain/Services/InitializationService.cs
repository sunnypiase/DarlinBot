using System.Collections.Concurrent;
using Darlin.Logging;
using Serilog;

namespace Darlin.Domain.Services;

public class InitializationService(TickerManager tm)
{
    private const int BatchSize = 50;
    private static readonly TimeSpan DelayBetweenBatches = TimeSpan.FromMinutes(1.25);

    public async Task RunAsync(CancellationToken ct)
    {
        var tickers = tm.Tickers;
        Log.Information(
            "{EventId}: Initializing {Count} tickers in batches of {BatchSize}",
            LogEvents.AppStart, tickers.Count, BatchSize);

        var success = new ConcurrentBag<string>();
        var failure = new ConcurrentBag<string>();
        var idx = 0;

        for (var i = 0; i < tickers.Count; i += BatchSize)
        {
            var batchNo = i / BatchSize + 1;
            var batch = tickers.Skip(i).Take(BatchSize).ToList();

            Log.Debug(
                "{EventId}: Starting batch {BatchNo} with {BatchCount} tickers",
                LogEvents.TickerInitAttempt, batchNo, batch.Count);

            var tasks = batch.Select(ticker =>
                Task.Run(async () =>
                {
                    var myId = Interlocked.Increment(ref idx);

                    for (var attempt = 1; attempt <= 5; attempt++)
                    {
                        Log.Debug(
                            "{EventId}: Ticker #{MyId} init {Name} ({Attempt}/5)",
                            LogEvents.TickerInitAttempt,
                            myId, ticker.Name, attempt);

                        if (await ticker.Initialize())
                        {
                            success.Add(ticker.Name);
                            Log.Information(
                                "{EventId}: Ticker #{MyId} initialized, starting loop",
                                LogEvents.TickerInitialized,
                                myId);

                            // start its event loop immediately
                            _ = Task.Run(async () =>
                            {
                                Log.Information(
                                    "{EventId}: Ticker #{MyId} loop started",
                                    LogEvents.TickerLoopStart,
                                    myId);
                                await ticker.Start();
                            }, ct);

                            return;
                        }

                        await Task.Delay(TimeSpan.FromSeconds(5), ct);
                    }

                    failure.Add(ticker.Name);
                    Log.Error(
                        "{EventId}: Ticker {Name} failed to initialize after retries",
                        LogEvents.TickerInitFailed,
                        ticker.Name);
                }, ct)
            ).ToArray();

            await Task.WhenAll(tasks);

            Log.Debug(
                "{EventId}: Batch {BatchNo} done. Waiting {Delay}",
                LogEvents.TickerLoopStart,
                batchNo, DelayBetweenBatches);

            await Task.Delay(DelayBetweenBatches, ct);
        }

        Log.Information(
            "{EventId}: Initialization complete: {SuccessCount}/{Total}",
            LogEvents.TickerInitialized,
            success.Count, tickers.Count);

        if (!failure.IsEmpty)
            Log.Warning(
                "{EventId}: Failed to initialize: {FailedList}",
                LogEvents.TickerInitFailed,
                string.Join(", ", failure));
    }
}