using System.Collections.Concurrent;
using System.Threading.Channels;
using Darlin.Domain.Enums;
using Darlin.Domain.Events;
using Darlin.Domain.Models;

namespace Darlin.Domain.Services;

public class OrderBlockManager : IDisposable
{
    private readonly ConcurrentDictionary<decimal, CancellationTokenSource> _ctsMap = new();
    private readonly int _isSignalTimeSeconds = 120;
    private readonly ConcurrentDictionary<decimal, OrderBlock> _orderBlocks = new();
    private readonly ChannelWriter<EventBase> _writer;
    private bool _disposed;

    public OrderBlockManager(ChannelWriter<EventBase> writer)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
    }

    public IReadOnlyDictionary<decimal, OrderBlock> OrderBlocks => _orderBlocks;

    /// <summary>
    ///     Cancels all pending timers and cleans up resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var cts in _ctsMap.Values)
        {
            cts.Cancel();
            cts.Dispose();
        }

        _ctsMap.Clear();
        _orderBlocks.Clear();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Adds a new OrderBlock and schedules a signal after the configured timeout.
    /// </summary>
    public void AddOrderBlock(OrderBlock ob)
    {
        if (ob == null) throw new ArgumentNullException(nameof(ob));

        // Only add if not already present
        if (_orderBlocks.TryAdd(ob.Price, ob))
        {
            var cts = new CancellationTokenSource();
            if (_ctsMap.TryAdd(ob.Price, cts))
                // Fire-and-forget delay task
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(_isSignalTimeSeconds), cts.Token);

                        // Only signal if still present and not already signaled
                        if (_orderBlocks.TryGetValue(ob.Price, out var block) && !block.IsSignal)
                        {
                            block.IsSignal = true;
                            _writer.TryWrite(new OrderBlockUpdatedEvent());
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // canceled — fine
                    }
                });
        }
    }

    /// <summary>
    ///     Removes an OrderBlock and cancels its pending signal task.
    /// </summary>
    public void RemoveOrderBlock(decimal price)
    {
        _orderBlocks.TryRemove(price, out _);

        if (_ctsMap.TryRemove(price, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }
    }

    /// <summary>
    ///     Returns the highest (for Bid) or lowest (for Ask) price OrderBlock
    ///     that has been marked as a signal.
    /// </summary>
    public OrderBlock? GetBestSignalOrderBlock(OrderBookSide side)
    {
        OrderBlock? best = null;

        foreach (var block in _orderBlocks.Values)
        {
            if (!block.IsSignal || block.Side != side)
                continue;

            if (best == null
                || (side == OrderBookSide.Bid && block.Price > best.Price)
                || (side == OrderBookSide.Ask && block.Price < best.Price))
                best = block;
        }

        return best;
    }
}