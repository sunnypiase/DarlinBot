using Darlin.Domain.Models;

namespace Darlin.Domain.Events;

public class PriceUpdateEvent(decimal bidPrice, decimal askPrice) : EventBase
{
    private decimal BidPrice { get; } = bidPrice;
    private decimal AskPrice { get; } = askPrice;

    public override void Handle(Ticker ticker)
    {
        if (ticker.BidPrice == BidPrice && ticker.AskPrice == AskPrice)
            return;

        ticker.BidPrice = BidPrice;
        ticker.AskPrice = AskPrice;
        ticker.OrderBookManager.UpdateOrderBookPrices(AskPrice, BidPrice);

        // Log.Verbose("{EventId}: {Ticker} PriceUpdate bid={BidPrice:F8} ask={AskPrice:F8}",
        //     LogEvents.PriceUpdated, ticker.Name, BidPrice, AskPrice);

        new OrderBookUpdatedEvent().Handle(ticker);
    }
}