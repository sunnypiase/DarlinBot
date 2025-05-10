namespace Darlin.Domain.Models;

/// <summary>
///     Represents a price level in the order book with price and volume.
/// </summary>
public class OrderBookLevel
{
    public decimal Price { get; set; }
    public decimal Volume { get; set; }
}