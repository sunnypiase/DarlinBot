using System.Collections.Concurrent;
using Darlin.Domain.Enums;

namespace Darlin.Domain.Models.Positions;

public class PendingPosition : PositionBase
{
    // Event raised when pending position meets the condition to convert.

    public PendingPosition(OrderBlock orderBlock, decimal pipSize)
        : base(orderBlock, pipSize)
    {
    }

    /// <summary>
    ///     Check if the pending position should trigger:
    ///     For long positions (Bid): trigger if current bid is less than (OrderBlock.Price + PipSize).
    ///     For short positions (Ask): trigger if current ask is greater than (OrderBlock.Price - PipSize).
    /// </summary>
    public bool ShouldOpen(decimal bid, decimal ask)
    {
        if (OrderBlock.Side == OrderBookSide.Bid) // long
        {
            if (ask <= OpenPrice && ask >= OrderBlock.Price) return true;
        }
        else if (OrderBlock.Side == OrderBookSide.Ask) // short
        {
            if (bid >= OpenPrice && bid <= OrderBlock.Price) return true;
        }

        return false;
    }

    public bool IsValid(ConcurrentDictionary<decimal, OrderBlock> orderBlocks)
    {
        if (orderBlocks.TryGetValue(OrderBlock.Price, out var orderBlock)) return true;
        return false;
    }

    // Converts the pending position to an open position and subscribes to price updates.
    public OpenPosition ToOpen(Ticker ticker)
    {
        var openPos = new OpenPosition(OrderBlock, PipSize, ticker.Threshold, ticker.Median, ticker.StdDev);
        return openPos;
    }
}