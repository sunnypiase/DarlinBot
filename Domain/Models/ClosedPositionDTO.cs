using Darlin.Domain.Enums;
using Darlin.Loggers;

namespace Darlin.Domain.Models;

public class ClosedPositionDTO
{
    public string TickerName { get; set; }
    public decimal TickerBidPrice { get; set; }
    public decimal TickerAskPrice { get; set; }
    public decimal PipSize { get; set; }
    public decimal OrderBlockPrice { get; set; }
    public decimal OrderBlockVolume { get; set; }
    public decimal OrderBlockVolumeOnOpen { get; set; }
    public decimal TresholdOnOpen { get; set; }
    public decimal MedianOnOpen { get; set; }
    public decimal StdDevOnOpen { get; set; }
    public OrderBookSide OrderBlockSide { get; set; }
    public DateTime OrderBlockCreationTime { get; set; }
    public TimeSpan OrderBlockLifeTimeOnOpen { get; set; }
    public decimal OpenPositionOpenPrice { get; set; }
    public DateTime OpenPositionOpenTime { get; set; }
    public decimal TakeProfit { get; set; }
    public decimal MaxProfitPrice { get; set; }
    public decimal StopLoss { get; set; }
    public decimal PositionSize { get; set; }
    public decimal ClosedPrice { get; set; }
    public decimal PnL { get; set; }
    public decimal MaxPotentialPnl { get; set; }
    public DateTime CloseTime { get; set; }
    public string CloseReason { get; set; }
    public OrderBookSnapshot OrderBookStateOnClose { get; set; }
}