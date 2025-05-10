namespace Darlin.Domain.Models;

/// <summary>
///     Represents a snapshot of the order book at a specific time.
/// </summary>
public class OrderBookSnapshot
{
    public DateTime SnapshotTime { get; set; } = DateTime.UtcNow;
    public List<OrderBookLevel> Asks { get; set; } = new();
    public List<OrderBookLevel> Bids { get; set; } = new();
}