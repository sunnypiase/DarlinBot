using Darlin.Domain.Enums;
using Darlin.Domain.Models;
using Darlin.Domain.Models.Positions;
using Darlin.Loggers;

namespace Darlin.Domain.Events;

public class OrderBlockUpdatedEvent : EventBase
{
    public override async ValueTask Handle(Ticker ticker)
    {
        if (ticker.OpenPosition == null)
        {
            var bestShortOrderBlock = ticker.OrderBlockManager.GetBestSignalOrderBlock(OrderBookSide.Ask);
            var bestLongOrderBlock = ticker.OrderBlockManager.GetBestSignalOrderBlock(OrderBookSide.Bid);

            if (ticker.PendingLong?.OrderBlock != bestLongOrderBlock)
            {
                ticker.PendingLong = bestLongOrderBlock != null
                    ? new PendingPosition(bestLongOrderBlock, ticker.PipSize)
                    : null;
                if (bestLongOrderBlock != null)
                {
                    // SimpleLogger.Log(ticker.Name + ": [PendingLong] " + bestLongOrderBlock.ToString());
                }
            }

            if (ticker.PendingShort?.OrderBlock != bestShortOrderBlock)
            {
                ticker.PendingShort = bestShortOrderBlock != null
                    ? new PendingPosition(bestShortOrderBlock, ticker.PipSize)
                    : null;
                if (bestShortOrderBlock != null)
                {
                    // SimpleLogger.Log(ticker.Name + ": [PendingShort] " + bestShortOrderBlock.ToString());
                }
            }

            if (ticker.OpenPosition == null &&
                (ticker.PendingLong?.ShouldOpen(ticker.BidPrice, ticker.AskPrice) ?? false))
            {
                var pos = ticker.PendingLong.ToOpen(ticker);
                ticker.OpenPosition = pos;
                ticker.PendingLong = null;
                ticker.PendingShort = null;
                // SimpleLogger.Log(ticker.Name + ": [OpenLong] " + ticker.OpenPosition.OrderBlock.ToString());
                ticker.OpenPositionFileLogger.Add(ticker.Name, new PositionInfo
                {
                    OpenTime = pos.OpenTime,
                    OpenPrice = pos.OpenPrice,
                    StopLossPrice = pos.StopLoss,
                    TakeProfitPrice = pos.TakeProfit,
                    OrderBlockPrice = pos.OrderBlock.Price,
                    PositionSize = pos.PositionSize,
                    OrderBookState = ticker.GetOrderBookSnapshot()
                });
            }

            if (ticker.OpenPosition == null &&
                (ticker.PendingShort?.ShouldOpen(ticker.BidPrice, ticker.AskPrice) ?? false))
            {
                var pos = ticker.PendingShort.ToOpen(ticker);
                ticker.OpenPosition = pos;
                ticker.PendingLong = null;
                ticker.PendingShort = null;
                // SimpleLogger.Log(ticker.Name + ": [OpenShort] " + ticker.OpenPosition.OrderBlock.ToString());
                ticker.OpenPositionFileLogger.Add(ticker.Name, new PositionInfo
                {
                    OpenTime = pos.OpenTime,
                    OpenPrice = pos.OpenPrice,
                    StopLossPrice = pos.StopLoss,
                    TakeProfitPrice = pos.TakeProfit,
                    OrderBlockPrice = pos.OrderBlock.Price,
                    PositionSize = pos.PositionSize,
                    OrderBookState = ticker.GetOrderBookSnapshot()
                });
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

                var closedDTO = new ClosedPositionDTO
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
                ticker.OpenPositionFileLogger.Remove(ticker.Name);
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

                var closedDTO = new ClosedPositionDTO
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
                ticker.OpenPositionFileLogger.Remove(ticker.Name);
            }
        }

        // Use the PositionLogger to log current positions.
        //ticker.PositionLogger.LogPositions(ticker);
        await Task.CompletedTask;
    }
}