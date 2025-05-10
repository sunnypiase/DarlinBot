namespace Darlin.Domain.Models;

/// <summary>
///     Model representing open position details.
/// </summary>
public class PositionInfo
{
    public DateTime OpenTime { get; set; }
    public decimal OpenPrice { get; set; }
    public decimal StopLossPrice { get; set; }
    public decimal TakeProfitPrice { get; set; }
    public decimal OrderBlockPrice { get; set; }
    public decimal PositionSize { get; set; }
    public OrderBookSnapshot OrderBookState { get; set; }
}