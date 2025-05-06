using System.Text;
using System.Threading.Channels;
using Darlin.DataRetrievers;
using Darlin.Domain.Enums;
using Darlin.Domain.Events;
using Darlin.Domain.Models.Positions;
using Darlin.Domain.Services;
using Darlin.Loggers;

namespace Darlin.Domain.Models;

public class Ticker
{
    private const int VolumeBufferSize = 288;
    private readonly BinanceExchangeInfoRetriever _exchangeInfoRetriever;

    private readonly ILogger<Ticker> _logger;
    private readonly BinanceOrderBookSnapshotRetriever _orderBookSnapshotRetriever;

    private readonly Lock _volumeLock = new();
    private readonly BinanceVolumeRetriever _volumeRetriever;
    private readonly Queue<decimal> _volumes = new(VolumeBufferSize);

    public Ticker(
        ILogger<Ticker> logger,
        BinanceExchangeInfoRetriever exchangeInfoRetriever,
        BinanceOrderBookSnapshotRetriever orderBookSnapshotRetriever,
        BinanceVolumeRetriever volumeRetriever)
    {
        _logger = logger;
        _exchangeInfoRetriever = exchangeInfoRetriever;
        _orderBookSnapshotRetriever = orderBookSnapshotRetriever;
        _volumeRetriever = volumeRetriever;

        EventChannel = Channel.CreateUnbounded<EventBase>();
    }

    public Channel<EventBase> EventChannel { get; }
    public ChannelWriter<EventBase> Writer => EventChannel.Writer;
    public ChannelReader<EventBase> Reader => EventChannel.Reader;

    public string Name { get; set; }
    public decimal Threshold { get; set; }
    public decimal Median { get; set; }
    public decimal StdDev { get; set; }
    public decimal BidPrice { get; set; }
    public decimal AskPrice { get; set; }
    public decimal PipSize { get; set; }
    public Action<ClosedPositionDTO> LogClosedPosition { get; set; }
    public OpenPositionFileLogger OpenPositionFileLogger { get; set; }

    public PendingPosition? PendingLong { get; set; }
    public PendingPosition? PendingShort { get; set; }
    public OpenPosition? OpenPosition { get; set; }

    public OrderBookManager OrderBookManager { get; set; }
    public OrderBlockManager OrderBlockManager { get; set; }

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
        try
        {
            OrderBlockManager = new OrderBlockManager(Writer);

            PipSize = await _exchangeInfoRetriever.GetPipSize(Name);
            var (ask, bid) = await _orderBookSnapshotRetriever.GetOrderBookSnapshot(Name, 1000);
            OrderBookManager = new OrderBookManager(ask, bid);

            var initialVolumes = await _volumeRetriever.GetVolumes(Name);
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
        catch (Exception e)
        {
            _logger.LogError($"ERROR init {Name}: {e}");
        }

        var ready = PipSize > 0 && OrderBookManager.AllLevels.Any() && Threshold > 0;
        if (ready)
        {
            _logger.LogInformation($"Ticker {Name} initialized (Threshold={Threshold})");
            return true;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Ticker Update Log - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"Name: {Name}");
        sb.AppendLine($"Threshold: {Threshold}");
        sb.AppendLine($"PipSize: {PipSize}");
        sb.AppendLine($"OrderBook Count: {OrderBookManager?.AllLevels?.Count()}");
        _logger.LogInformation($"ERROR: Ticker {Name} initialization failed: {sb}");
        return false;
    }

    public async Task Start()
    {
        _logger.LogInformation($"Ticker {Name} starting event loop");
        try
        {
            await foreach (var ev in Reader.ReadAllAsync()) await ev.Handle(this);
        }
        catch (Exception e)
        {
            _logger.LogInformation($"Ticker {Name} loop error: {e}");
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
        var median = BinanceVolumeRetriever.CalculateMedian([.. volumes]);
        var stdDev = BinanceVolumeRetriever.CalculateStdDev([.. volumes]);
        Threshold = median + stdDev * 1;
        Median = median;
        StdDev = stdDev;
    }
}