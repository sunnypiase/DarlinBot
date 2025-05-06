using Darlin.Domain.Models;

namespace Darlin.Domain.Events;

public class PriceUpdateEvent(decimal BidPrice, decimal AskPrice) : EventBase
{
    public override async ValueTask Handle(Ticker ticker)
    {
        if (ticker.BidPrice == BidPrice && ticker.AskPrice == AskPrice) return;
        ticker.BidPrice = BidPrice;
        ticker.AskPrice = AskPrice;
        ticker.OrderBookManager.UpdateOrderBookPrices(AskPrice, BidPrice);
        await new OrderBookUpdatedEvent().Handle(ticker);

        // Log the price update.
        //ticker.TickerLogger.LogPriceUpdate(ticker);
    }
}