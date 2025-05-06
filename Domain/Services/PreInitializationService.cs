using System.Collections.Concurrent;
using System.Text;
using Binance.Net;
using Binance.Net.Clients;
using Binance.Net.Enums;
using Darlin.DataRetrievers;
using Darlin.Domain.Events;
using Darlin.Domain.Models;
using Darlin.Loggers;
using Newtonsoft.Json;

namespace Darlin.Domain.Services;

public class PreInitializationService
{
    private const int MaxBytes = 1000;
    private const int TopN = 30;
    private readonly CsvLogger<ClosedPositionDTO> _csv;
    private readonly BinanceDayTickerStatsRetriever _dayStats;
    private readonly BinanceExchangeInfoRetriever _info;
    private readonly ILoggerFactory _lf;
    private readonly ILogger<PreInitializationService> _logger;
    private readonly OpenPositionFileLogger _openFile;
    private readonly BinanceOrderBookSnapshotRetriever _snapshot;
    private readonly BinanceSocketClient _socket;
    private readonly TickerManager _tm;
    private readonly BinanceVolumeRetriever _volume;

    public PreInitializationService(
        ILogger<PreInitializationService> logger,
        ILoggerFactory lf,
        BinanceDayTickerStatsRetriever dayStats,
        BinanceExchangeInfoRetriever info,
        BinanceOrderBookSnapshotRetriever snapshot,
        BinanceVolumeRetriever volume,
        OpenPositionFileLogger openFile,
        CsvLogger<ClosedPositionDTO> csv,
        BinanceSocketClient socket,
        TickerManager tm)
    {
        _logger = logger;
        _lf = lf;
        _dayStats = dayStats;
        _info = info;
        _snapshot = snapshot;
        _volume = volume;
        _openFile = openFile;
        _csv = csv;
        _socket = socket;
        _tm = tm;

        // rate-limit logging
        BinanceExchange.RateLimiter.RateLimitTriggered += evt =>
            _logger.LogInformation("Rate limit hit: {Evt}", evt);
    }

    public async Task RunAsync(CancellationToken сancellationToken)
    {
        _logger.LogInformation("Fetching top {TopN} tickers…", TopN);
        var symbols = await _dayStats.GetTopTickersByVolume(TopN);

        _logger.LogInformation("Creating {Count} Ticker instances…", symbols.Count);
        var tickers = symbols.Select(sym =>
        {
            var t = new Ticker(
                _lf.CreateLogger<Ticker>(),
                _info,
                _snapshot,
                _volume
            );
            t.Name = sym;
            t.LogClosedPosition = _csv.Log;
            t.OpenPositionFileLogger = _openFile;
            return t;
        }).ToList();

        var map = new ConcurrentDictionary<string, Ticker>(
            tickers.ToDictionary(t => t.Name, t => t)
        );
        _tm.SetTickers(tickers, map);

        _logger.LogInformation("Subscribing to Binance streams…");
        var batches = BatchByJsonByteLimit(symbols, MaxBytes);
        foreach (var batch in batches)
        {
            _ = SubscribePriceWithRetryAsync(batch);
            _ = SubscribeDepthWithRetryAsync(batch);
            _ = SubscribeKlineWithRetryAsync(batch, KlineInterval.FiveMinutes);
        }
    }

    private async Task SubscribePriceWithRetryAsync(List<string> batch)
    {
        var label = string.Join(',', batch);
        while (true)
        {
            var res = await _socket
                .UsdFuturesApi
                .ExchangeData
                .SubscribeToBookTickerUpdatesAsync(batch, data =>
                {
                    if (_tm.TickerMap.TryGetValue(data.Data.Symbol, out var t))
                        t.Writer.TryWrite(new PriceUpdateEvent(
                            data.Data.BestBidPrice,
                            data.Data.BestAskPrice
                        ));
                });

            if (!res.Success)
            {
                _logger.LogWarning("[PRICE] {L} err {E}, retrying…",
                    label, res.Error);
                await Task.Delay(5000);
                continue;
            }

            _logger.LogInformation("[PRICE] {L} ✅", label);
            res.Data.ConnectionClosed += () =>
            {
                _logger.LogInformation("[PRICE] {L} disconnected → re-sub", label);
                _ = SubscribePriceWithRetryAsync(batch);
            };
            break;
        }
    }

    private async Task SubscribeDepthWithRetryAsync(List<string> batch)
    {
        var label = string.Join(',', batch);
        while (true)
        {
            var res = await _socket
                .UsdFuturesApi
                .ExchangeData
                .SubscribeToOrderBookUpdatesAsync(batch, 100, data =>
                {
                    if (!_tm.TickerMap.TryGetValue(data.Data.Symbol, out var t)) return;
                    var asks = data.Data.Asks
                        .Select(a => new KeyValuePair<decimal, decimal>(a.Price, a.Quantity));
                    var bids = data.Data.Bids
                        .Select(b => new KeyValuePair<decimal, decimal>(b.Price, b.Quantity));
                    t.Writer.TryWrite(new OrderBookValuesUpdate(asks, bids)
                    {
                        EventTime = data.Data.EventTime
                    });
                });

            if (!res.Success)
            {
                _logger.LogWarning("[DEPTH] {L} err {E}, retrying…",
                    label, res.Error);
                await Task.Delay(5000);
                continue;
            }

            _logger.LogInformation("[DEPTH] {L} ✅", label);
            res.Data.ConnectionClosed += () =>
            {
                _logger.LogInformation("[DEPTH] {L} disconnected → re-sub", label);
                _ = SubscribeDepthWithRetryAsync(batch);
            };
            break;
        }
    }

    private async Task SubscribeKlineWithRetryAsync(
        List<string> batch, KlineInterval interval)
    {
        var label = string.Join(',', batch);
        while (true)
        {
            var res = await _socket
                .UsdFuturesApi
                .ExchangeData
                .SubscribeToKlineUpdatesAsync(batch, interval, data =>
                {
                    if ((data?.Data?.Data?.Final ?? false)
                        && _tm.TickerMap.TryGetValue(data.Data.Symbol, out var t))
                        t.Writer.TryWrite(
                            new TresholdEvent(data.Data.Data.Volume)
                        );
                });

            if (!res.Success)
            {
                _logger.LogWarning("[KLINE] {L} err {E}, retrying…",
                    label, res.Error);
                await Task.Delay(5000);
                continue;
            }

            _logger.LogInformation("[KLINE] {L} ✅", label);
            res.Data.ConnectionClosed += () =>
            {
                _logger.LogInformation("[KLINE] {L} disconnected → re-sub", label);
                _ = SubscribeKlineWithRetryAsync(batch, interval);
            };
            break;
        }
    }

    private static List<List<string>> BatchByJsonByteLimit(
        IEnumerable<string> items, int maxBytes)
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
                batches.Add(new List<string>(curr));
                curr.Clear();
                curr.Add(it);
            }
        }

        if (curr.Any()) batches.Add(curr);
        return batches;
    }
}