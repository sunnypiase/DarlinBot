using Darlin.Domain.Enums;

namespace Darlin.Domain.Models.Positions;

public abstract class PositionBase
{
    protected PositionBase(OrderBlock orderBlock, decimal pipSize)
    {
        OrderBlock = orderBlock;
        PipSize = pipSize;
        // For a long (Bid) position, we set open price slightly above order block price;
        // for a short (Ask) position, slightly below.
        OpenPrice = OrderBlock.Side == OrderBookSide.Ask
            ? OrderBlock.Price - pipSize
            : OrderBlock.Price + pipSize;
    }

    public OrderBlock OrderBlock { get; set; }
    public decimal OpenPrice { get; set; }
    public decimal PipSize { get; set; }
}
//Position Type	Order Action	Trigger Condition	Order Execution Price
//Long	Open (Buy)	Price reaches your long entry level	Ask Price
//Long	Close (Sell - Stop Loss)	Price falls to your stop loss level	Bid Price
//Long	Close (Sell - Take Profit)	Price rises to your take profit level	Bid Price
//Short	Open (Sell)	Price reaches your short entry level	Bid Price
//Short	Close (Buy - Stop Loss)	Price rises to your stop loss level (for shorts)	Ask Price
//Short	Close (Buy - Take Profit)	Price falls to your take profit level (for shorts)	Ask Price