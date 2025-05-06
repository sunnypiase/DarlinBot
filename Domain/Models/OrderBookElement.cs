using Darlin.Domain.Enums;

namespace Darlin.Domain.Models;

public struct OrderBookElement(decimal Price, decimal Volume, OrderBookSide Side)
{
    public decimal Price { get; set; } = Price;
    public decimal Volume { get; set; } = Volume;
    public OrderBookSide Side { get; set; } = Side;
}