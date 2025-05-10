using System.Text;
using System.Threading.Channels;
using Darlin.DataRetrievers;
using Darlin.Domain.Enums;
using Darlin.Domain.Events;
using Darlin.Domain.Models.Positions;
using Darlin.Domain.Services;
using Darlin.Logging;
using Serilog;

namespace Darlin.Domain.Models;

public class Ticker(
    BinanceExchangeInfoRetriever exchangeInfoRetriever,
    BinanceOrderBookSnapshotRetriever orderBookSnapshotRetriever,
    BinanceVolumeRetriever volumeRetriever)
{
    private const int VolumeBufferSize = 288 * 5;

    private readonly Lock _volumeLock = new();
    private readonly Queue<decimal> _volumes = new(VolumeBufferSize);

    private Channel<EventBase> EventChannel { get; } = Channel.CreateUnbounded<EventBase>();
    public ChannelWriter<EventBase> Writer => EventChannel.Writer;
    private ChannelReader<EventBase> Reader => EventChannel.Reader;

    public required string Name { get; set; }
    public decimal Threshold { get; set; }
    public decimal Median { get; set; }
    public decimal StdDev { get; set; }
    public decimal BidPrice { get; set; }
    public decimal AskPrice { get; set; }
    public decimal PipSize { get; set; }
    public Action<ClosedPositionDto> LogClosedPosition { get; set; }

    public PendingPosition? PendingLong { get; set; }
    public PendingPosition? PendingShort { get; set; }
    public OpenPosition? OpenPosition { get; set; }

    public OrderBookManager OrderBookManager { get; set; } = null!;
    public OrderBlockManager OrderBlockManager { get; set; } = null!;

    public OrderBookSnapshot GetOrderBookSnapshot()
    {
        var snapshot = new OrderBookSnapshot();
        var allLevels = OrderBookManager.AllLevels.ToList();

        snapshot.Asks = allLevels
            .Where(level => level.Side == OrderBookSide.Ask)
            .OrderBy(level => level.Price)
            .Take(100)
            .Select(level => new OrderBookLevel
            {
                Price = level.Price,
                Volume = level.Volume
            })
            .ToList();

        snapshot.Bids = allLevels
            .Where(level => level.Side == OrderBookSide.Bid)
            .OrderByDescending(level => level.Price)
            .Take(100)
            .Select(level => new OrderBookLevel
            {
                Price = level.Price,
                Volume = level.Volume
            })
            .ToList();

        return snapshot;
    }

    public async Task<bool> Initialize()
    {
        Log.Information("{EventId}: Initializing {Ticker}",
            LogEvents.TickerInitAttempt, Name);

        try
        {
            OrderBlockManager = new OrderBlockManager(Writer);

            PipSize = await exchangeInfoRetriever.GetPipSize(Name);
            var (ask, bid) = await orderBookSnapshotRetriever.GetOrderBookSnapshot(Name, 1000);
            OrderBookManager = new OrderBookManager(ask, bid);

            var initialVolumes = await volumeRetriever.GetVolumes(Name);
            lock (_volumeLock)
            {
                foreach (var vol in initialVolumes)
                {
                    _volumes.Enqueue(vol);
                    if (_volumes.Count > VolumeBufferSize) _volumes.Dequeue();
                }

                SetCalculatedThreshold(_volumes);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "{EventId}: {Ticker} init error",
                LogEvents.TickerInitFailed, Name);
        }

        var ready = PipSize > 0 && OrderBookManager.AllLevels.Any() && Threshold > 0;
        if (ready)
        {
            Log.Information("{EventId}: {Ticker} initialized (Threshold={Threshold:F4})",
                LogEvents.TickerInitialized, Name, Threshold);
            return true;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Ticker Update Log - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"Name: {Name}");
        sb.AppendLine($"Threshold: {Threshold}");
        sb.AppendLine($"PipSize: {PipSize}");
        sb.AppendLine($"OrderBook Count: {OrderBookManager?.AllLevels?.Count()}");
        Log.Warning("{EventId}: {Ticker} initialization incomplete",
            LogEvents.TickerInitFailed, sb);
        return false;
    }

    public async Task Start()
    {
        Log.Information("{EventId}: {Ticker} loop starting",
            LogEvents.TickerLoopStart, Name);
        try
        {
            await foreach (var ev in Reader.ReadAllAsync()) await ev.Handle(this);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "{EventId}: {Ticker} loop error",
                LogEvents.TickerLoopError, Name);
        }
    }

    public void UpdateThreshold(decimal newVol)
    {
        lock (_volumeLock)
        {
            _volumes.Enqueue(newVol);
            if (_volumes.Count > VolumeBufferSize) _volumes.Dequeue();
            SetCalculatedThreshold(_volumes);
        }
    }

    private void SetCalculatedThreshold(IEnumerable<decimal> volumes)
    {
        var vols = volumes as decimal[] ?? volumes.ToArray();
        var median = BinanceVolumeRetriever.CalculateMedian([.. vols]);
        var stdDev = BinanceVolumeRetriever.CalculateStdDev([.. vols]);
        Threshold = median + stdDev * 1;
        Median = median;
        StdDev = stdDev;
    }
}