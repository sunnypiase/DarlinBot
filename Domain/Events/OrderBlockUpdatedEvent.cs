using Darlin.Domain.Enums;
using Darlin.Domain.Models;
using Darlin.Domain.Models.Positions;
using Darlin.Logging;
using Serilog;

namespace Darlin.Domain.Events;

public class OrderBlockUpdatedEvent : EventBase
{
    public override async ValueTask Handle(Ticker ticker)
    {
        if (ticker.OpenPosition == null)
        {
            var bestLong = ticker.OrderBlockManager.GetBestSignalOrderBlock(OrderBookSide.Bid);
            var bestShort = ticker.OrderBlockManager.GetBestSignalOrderBlock(OrderBookSide.Ask);

            if (ticker.PendingLong?.OrderBlock != bestLong)
                ticker.PendingLong = bestLong is null
                    ? null
                    : new PendingPosition(bestLong, ticker.PipSize);
            // if (bestLong is not null)
            //     Log.Debug("{EventId}: {Ticker} PendingLong → {@OrderBlock}",
            //         LogEvents.PendingLong, ticker.Name, bestLong);
            if (ticker.PendingShort?.OrderBlock != bestShort)
                ticker.PendingShort = bestShort is null
                    ? null
                    : new PendingPosition(bestShort, ticker.PipSize);
            // if (bestShort is not null)
            //     Log.Debug("{EventId}: {Ticker} PendingShort → {@OrderBlock}",
            //         LogEvents.PendingShort, ticker.Name, bestShort);
            if (ticker.OpenPosition == null &&
                (ticker.PendingLong?.ShouldOpen(ticker.BidPrice, ticker.AskPrice) ?? false))
            {
                var pos = ticker.PendingLong.ToOpen(ticker);
                ticker.OpenPosition = pos;
                ticker.PendingLong = null;
                ticker.PendingShort = null;
                Log.Information("{EventId}: {Ticker} OpenLong @ {OpenPrice} vol={Volume}",
                    LogEvents.OpenPosition, ticker.Name, pos.OrderBlock.Price, pos.PositionSize);
            }

            if (ticker.OpenPosition == null &&
                (ticker.PendingShort?.ShouldOpen(ticker.BidPrice, ticker.AskPrice) ?? false))
            {
                var pos = ticker.PendingShort.ToOpen(ticker);
                ticker.OpenPosition = pos;
                ticker.PendingLong = null;
                ticker.PendingShort = null;
                Log.Information("{EventId}: {Ticker} OpenShort @ {OpenPrice} vol={Volume}",
                    LogEvents.OpenPosition, ticker.Name, pos.OrderBlock.Price, pos.PositionSize);
            }
        }

        if (ticker.OpenPosition != null)
        {
            if (ticker.OpenPosition.IsReachStopLoss(ticker.BidPrice, ticker.AskPrice))
            {
                var closingPrice = ticker.OpenPosition.StopLoss;
                var coins = ticker.OpenPosition.PositionSize / ticker.OpenPosition.OpenPrice;
                var pnl = ticker.OpenPosition.OrderBlock.Side == OrderBookSide.Bid
                    ? (closingPrice - ticker.OpenPosition.OpenPrice) * coins
                    : (ticker.OpenPosition.OpenPrice - closingPrice) * coins;
                var commissionCost = 2 * ticker.OpenPosition.PositionSize * (OpenPosition.CommissionPct / 100m);
                pnl -= commissionCost;

                var maxPotentialPnl = ticker.OpenPosition.OrderBlock.Side == OrderBookSide.Bid
                    ? (ticker.OpenPosition.MaxProfitPrice - ticker.OpenPosition.OpenPrice) * coins
                    : (ticker.OpenPosition.OpenPrice - ticker.OpenPosition.MaxProfitPrice) * coins;
                maxPotentialPnl -= commissionCost;

                var closedDTO = new ClosedPositionDto
                {
                    TickerName = ticker.Name,
                    TickerBidPrice = ticker.BidPrice,
                    TickerAskPrice = ticker.AskPrice,
                    PipSize = ticker.PipSize,
                    OrderBlockPrice = ticker.OpenPosition.OrderBlock.Price,
                    OrderBlockVolume = ticker.OpenPosition.OrderBlock.Volume,
                    OrderBlockVolumeOnOpen = ticker.OpenPosition.OrderBlockVolumeOnOpen,
                    TresholdOnOpen = ticker.OpenPosition.TresholdOnOpen,
                    MedianOnOpen = ticker.OpenPosition.MedianOnOpen,
                    StdDevOnOpen = ticker.OpenPosition.StdDevOnOpen,
                    OrderBlockSide = ticker.OpenPosition.OrderBlock.Side,
                    OrderBlockCreationTime = ticker.OpenPosition.OrderBlock.CreationTime,
                    OrderBlockLifeTimeOnOpen = ticker.OpenPosition.OrderBlockLifeTimeOnOpen,
                    OpenPositionOpenPrice = ticker.OpenPosition.OpenPrice,
                    OpenPositionOpenTime = ticker.OpenPosition.OpenTime,
                    TakeProfit = ticker.OpenPosition.TakeProfit,
                    MaxProfitPrice = ticker.OpenPosition.MaxProfitPrice,
                    StopLoss = ticker.OpenPosition.StopLoss,
                    PositionSize = ticker.OpenPosition.PositionSize,
                    ClosedPrice = closingPrice,
                    PnL = pnl,
                    MaxPotentialPnl = maxPotentialPnl,
                    CloseTime = DateTime.UtcNow,
                    CloseReason = "StopLoss",
                    OrderBookStateOnClose = ticker.GetOrderBookSnapshot()
                };
                ticker.LogClosedPosition(closedDTO);
                ticker.OpenPosition = null;
                Log.Information("{EventId}: {Ticker} Closed {Reason} @ {Price} PnL={Pnl:F4}",
                    LogEvents.ClosePosition,
                    ticker.Name,
                    closedDTO.CloseReason,
                    closingPrice,
                    pnl);
            }
            else if (ticker.OpenPosition.IsReachTakeProfit(ticker.BidPrice, ticker.AskPrice))
            {
                var closingPrice = ticker.OpenPosition.TakeProfit;
                var coins = ticker.OpenPosition.PositionSize / ticker.OpenPosition.OpenPrice;
                var pnl = ticker.OpenPosition.OrderBlock.Side == OrderBookSide.Bid
                    ? (closingPrice - ticker.OpenPosition.OpenPrice) * coins
                    : (ticker.OpenPosition.OpenPrice - closingPrice) * coins;
                var commissionCost = 2 * ticker.OpenPosition.PositionSize * (OpenPosition.CommissionPct / 100m);
                pnl -= commissionCost;

                var closedDTO = new ClosedPositionDto
                {
                    TickerName = ticker.Name,
                    TickerBidPrice = ticker.BidPrice,
                    TickerAskPrice = ticker.AskPrice,
                    PipSize = ticker.PipSize,
                    OrderBlockPrice = ticker.OpenPosition.OrderBlock.Price,
                    OrderBlockVolume = ticker.OpenPosition.OrderBlock.Volume,
                    OrderBlockSide = ticker.OpenPosition.OrderBlock.Side,
                    OrderBlockCreationTime = ticker.OpenPosition.OrderBlock.CreationTime,
                    OrderBlockLifeTimeOnOpen = ticker.OpenPosition.OrderBlockLifeTimeOnOpen,
                    OpenPositionOpenPrice = ticker.OpenPosition.OpenPrice,
                    OpenPositionOpenTime = ticker.OpenPosition.OpenTime,
                    OrderBlockVolumeOnOpen = ticker.OpenPosition.OrderBlockVolumeOnOpen,
                    TresholdOnOpen = ticker.OpenPosition.TresholdOnOpen,
                    MedianOnOpen = ticker.OpenPosition.MedianOnOpen,
                    StdDevOnOpen = ticker.OpenPosition.StdDevOnOpen,
                    TakeProfit = ticker.OpenPosition.TakeProfit,
                    MaxProfitPrice = ticker.OpenPosition.MaxProfitPrice,
                    StopLoss = ticker.OpenPosition.StopLoss,
                    PositionSize = ticker.OpenPosition.PositionSize,
                    ClosedPrice = closingPrice,
                    PnL = pnl,
                    MaxPotentialPnl = pnl,
                    CloseTime = DateTime.UtcNow,
                    CloseReason = "TakeProfit",
                    OrderBookStateOnClose = ticker.GetOrderBookSnapshot()
                };
                ticker.LogClosedPosition(closedDTO);
                ticker.OpenPosition = null;
                Log.Information("{EventId}: {Ticker} Closed {Reason} @ {Price} PnL={Pnl:F4}",
                    LogEvents.ClosePosition,
                    ticker.Name,
                    closedDTO.CloseReason,
                    closingPrice,
                    pnl);
            }
        }

        await Task.CompletedTask;
    }
}