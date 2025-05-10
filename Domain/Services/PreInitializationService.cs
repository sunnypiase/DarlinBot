using System.Collections.Concurrent;
using System.Text;
using Binance.Net;
using Binance.Net.Clients;
using Binance.Net.Enums;
using Darlin.DataRetrievers;
using Darlin.Domain.Events;
using Darlin.Domain.Models;
using Darlin.Loggers;
using Darlin.Logging;
using Newtonsoft.Json;
using Serilog;

namespace Darlin.Domain.Services;

public class PreInitializationService
{
    private const int MaxBytes = 1000;
    private const int TopN = 300;
    private readonly IClosedPositionLogger _csv;

    private readonly BinanceDayTickerStatsRetriever _dayStats;
    private readonly BinanceExchangeInfoRetriever _info;
    private readonly BinanceOrderBookSnapshotRetriever _snapshot;
    private readonly BinanceSocketClient _socket;
    private readonly TickerManager _tm;
    private readonly BinanceVolumeRetriever _volume;

    public PreInitializationService(
        BinanceDayTickerStatsRetriever dayStats,
        BinanceExchangeInfoRetriever info,
        BinanceOrderBookSnapshotRetriever snapshot,
        BinanceVolumeRetriever volume,
        IClosedPositionLogger csv,
        BinanceSocketClient socket,
        TickerManager tm)
    {
        _dayStats = dayStats;
        _info = info;
        _snapshot = snapshot;
        _volume = volume;
        _csv = csv;
        _socket = socket;
        _tm = tm;

        // rate-limit logging
        BinanceExchange.RateLimiter.RateLimitTriggered += evt =>
            Log.Information("{EventId}: Rate limit hit: {Evt}",
                LogEvents.SubscriptionRetry, evt);
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        Log.Information("{EventId}: Fetching top {TopN} tickers…",
            LogEvents.AppStart, TopN);
        var symbols = await _dayStats.GetTopTickersByVolume(TopN);

        Log.Information("{EventId}: Creating {Count} Ticker instances…",
            LogEvents.AppStart, symbols.Count);
        var tickers = symbols.Select(sym =>
        {
            var t = new Ticker(
                _info,
                _snapshot,
                _volume
            )
            {
                Name = sym,
                LogClosedPosition = _csv.Log
            };
            return t;
        }).ToList();

        var map = new ConcurrentDictionary<string, Ticker>(
            tickers.ToDictionary(t => t.Name, t => t)
        );
        _tm.SetTickers(tickers, map);

        Log.Information("{EventId}: Subscribing to Binance streams…",
            LogEvents.SubscriptionSuccess);
        var batches = BatchByJsonByteLimit(symbols, MaxBytes);
        foreach (var batch in batches)
        {
            _ = SubscribePriceWithRetryAsync(batch, cancellationToken);
            _ = SubscribeDepthWithRetryAsync(batch, cancellationToken);
            _ = SubscribeKlineWithRetryAsync(batch, KlineInterval.FiveMinutes, cancellationToken);
        }
    }

    private async Task SubscribePriceWithRetryAsync(List<string> batch, CancellationToken ct)
    {
        var label = string.Join(',', batch);
        while (true)
        {
            var res = await _socket
                .UsdFuturesApi.ExchangeData
                .SubscribeToBookTickerUpdatesAsync(batch, data =>
                {
                    if (_tm.TickerMap.TryGetValue(data.Data.Symbol, out var t))
                        t.Writer.TryWrite(new PriceUpdateEvent(
                            data.Data.BestBidPrice,
                            data.Data.BestAskPrice
                        ));
                }, ct);

            if (!res.Success)
            {
                Log.Warning("{EventId}: [PRICE] {Label} error {Error}, retrying…",
                    LogEvents.SubscriptionRetry, label, res.Error);
                await Task.Delay(5000, ct);
                continue;
            }

            Log.Information("{EventId}: [PRICE] {Label} subscribed",
                LogEvents.SubscriptionSuccess, label);
            res.Data.ConnectionClosed += () =>
            {
                Log.Warning("{EventId}: [PRICE] {Label} disconnected → retry",
                    LogEvents.SubscriptionRetry, label);
                _ = SubscribePriceWithRetryAsync(batch, ct);
            };
            break;
        }
    }

    private async Task SubscribeDepthWithRetryAsync(List<string> batch, CancellationToken ct)
    {
        var label = string.Join(',', batch);
        while (true)
        {
            var res = await _socket
                .UsdFuturesApi.ExchangeData
                .SubscribeToOrderBookUpdatesAsync(batch, 100, data =>
                {
                    if (_tm.TickerMap.TryGetValue(data.Data.Symbol, out var t))
                    {
                        var asks = data.Data.Asks
                            .Select(a => new KeyValuePair<decimal, decimal>(a.Price, a.Quantity));
                        var bids = data.Data.Bids
                            .Select(b => new KeyValuePair<decimal, decimal>(b.Price, b.Quantity));
                        t.Writer.TryWrite(new OrderBookValuesUpdate(asks, bids)
                        {
                            EventTime = data.Data.EventTime
                        });
                    }
                }, ct);

            if (!res.Success)
            {
                Log.Warning("{EventId}: [DEPTH] {Label} error {Error}, retrying…",
                    LogEvents.SubscriptionRetry, label, res.Error);
                await Task.Delay(5000, ct);
                continue;
            }

            Log.Information("{EventId}: [DEPTH] {Label} subscribed",
                LogEvents.SubscriptionSuccess, label);
            res.Data.ConnectionClosed += () =>
            {
                Log.Warning("{EventId}: [DEPTH] {Label} disconnected → retry",
                    LogEvents.SubscriptionRetry, label);
                _ = SubscribeDepthWithRetryAsync(batch, ct);
            };
            break;
        }
    }

    private async Task SubscribeKlineWithRetryAsync(
        List<string> batch,
        KlineInterval interval,
        CancellationToken ct)
    {
        var label = string.Join(',', batch);
        while (true)
        {
            var res = await _socket
                .UsdFuturesApi.ExchangeData
                .SubscribeToKlineUpdatesAsync(batch, interval, data =>
                {
                    if ((data?.Data?.Data?.Final ?? false)
                        && _tm.TickerMap.TryGetValue(data.Data.Symbol, out var t))
                        t.Writer.TryWrite(
                            new TresholdEvent(data.Data.Data.Volume)
                        );
                }, ct: ct);

            if (!res.Success)
            {
                Log.Warning("{EventId}: [KLINE] {Label} error {Error}, retrying…",
                    LogEvents.SubscriptionRetry, label, res.Error);
                await Task.Delay(5000, ct);
                continue;
            }

            Log.Information("{EventId}: [KLINE] {Label} subscribed",
                LogEvents.SubscriptionSuccess, label);
            res.Data.ConnectionClosed += () =>
            {
                Log.Warning("{EventId}: [KLINE] {Label} disconnected → retry",
                    LogEvents.SubscriptionRetry, label);
                _ = SubscribeKlineWithRetryAsync(batch, interval, ct);
            };
            break;
        }
    }

    private static List<List<string>> BatchByJsonByteLimit(
        IEnumerable<string> items,
        int maxBytes)
    {
        var batches = new List<List<string>>();
        var curr = new List<string>();
        foreach (var it in items)
        {
            curr.Add(it);
            var size = Encoding.UTF8.GetByteCount(
                JsonConvert.SerializeObject(curr));
            if (size > maxBytes)
            {
                curr.RemoveAt(curr.Count - 1);
                batches.Add([..curr]);
                curr.Clear();
                curr.Add(it);
            }
        }

        if (curr.Any()) batches.Add(curr);
        return batches;
    }
}