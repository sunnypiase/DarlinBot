using Darlin.Domain.Enums;

namespace Darlin.Domain.Models.Positions;

public class OpenPosition : PositionBase
{
    public const decimal MaxLoss = 5m; // Maximum loss in USD per trade
    public const decimal Capital = 10_000m; // Total capital in USD
    public const decimal CommissionPct = 0.05m; // Commission percent

    public OpenPosition(OrderBlock orderBlock, decimal pipSize, decimal tresholdOnOpen, decimal medianOnOpen,
        decimal stdDevOnOpen)
        : base(orderBlock, pipSize)
    {
        if (OrderBlock.Side == OrderBookSide.Bid) // long position
            StopLoss = OrderBlock.Price - pipSize;
        else if (OrderBlock.Side == OrderBookSide.Ask) // short position
            StopLoss = OrderBlock.Price + pipSize;
        var d = Math.Abs(OpenPrice - StopLoss);

        // Commission factor per side (e.g., 0.1% becomes 0.001)
        var commFactor = CommissionPct / 100m;

        // Calculate PositionSize so that hitting StopLoss gives a net loss of $5 (MaxLoss)
        PositionSize = MaxLoss / (d / OpenPrice + 2 * commFactor);

        TakeProfit = OrderBlock.Side switch
        {
            // Long position
            OrderBookSide.Bid => OpenPrice * (1m + MaxLoss * 20 / PositionSize + 2 * commFactor),
            // Short position
            OrderBookSide.Ask => OpenPrice * (1m - MaxLoss * 20 / PositionSize - 2 * commFactor),
            _ => TakeProfit
        };

        PositionSize = PositionSize <= Capital ? PositionSize : Capital;
        OrderBlockVolumeOnOpen = OrderBlock.Volume;
        TresholdOnOpen = tresholdOnOpen;
        MedianOnOpen = medianOnOpen;
        StdDevOnOpen = stdDevOnOpen;
        OrderBlockLifeTimeOnOpen = DateTime.UtcNow - OrderBlock.CreationTime;
        MaxProfitPrice = OpenPrice;
    }

    public DateTime OpenTime { get; set; } = DateTime.UtcNow;
    public decimal TakeProfit { get; }
    public decimal StopLoss { get; }
    public decimal PositionSize { get; } // in USD

    public decimal TresholdOnOpen { get; private set; }
    public decimal MedianOnOpen { get; private set; }
    public decimal StdDevOnOpen { get; private set; }
    public decimal OrderBlockVolumeOnOpen { get; private set; }
    public decimal MaxProfitPrice { get; set; }
    public TimeSpan OrderBlockLifeTimeOnOpen { get; private set; }

    internal bool IsReachStopLoss(decimal bidPrice, decimal askPrice)
    {
        return OrderBlock.Side switch
        {
            // long
            OrderBookSide.Bid => bidPrice <= StopLoss,
            // short
            OrderBookSide.Ask => askPrice >= StopLoss,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    internal bool IsReachTakeProfit(decimal bidPrice, decimal askPrice)
    {
        switch (OrderBlock.Side)
        {
            case OrderBookSide.Bid: // long
                MaxProfitPrice = Math.Max(MaxProfitPrice, bidPrice);
                return bidPrice >= TakeProfit;

            case OrderBookSide.Ask: // short
                MaxProfitPrice = Math.Min(MaxProfitPrice, askPrice);
                return askPrice <= TakeProfit;

            default:
                throw new ArgumentOutOfRangeException(nameof(OrderBlock.Side), "Invalid order side.");
        }
    }
}