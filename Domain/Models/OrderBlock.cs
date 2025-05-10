using Darlin.Domain.Enums;

namespace Darlin.Domain.Models;

public class OrderBlock
{
    public decimal Price { get; init; }
    public decimal Volume { get; init; }
    public OrderBookSide Side { get; init; } // Bid is long, Ask is short

    public DateTime CreationTime { get; set; } = DateTime.UtcNow;

    // private uint LifeTime { get; set; } = 0;
    public bool IsSignal { get; set; }


    public override bool Equals(object? obj)
    {
        return obj is OrderBlock block && Price == block.Price;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Price);
    }

    public override string ToString()
    {
        return $"{Price}, {Volume}, {Side}";
    }
}