using Darlin.Domain.Models;

namespace Darlin.Domain.Events;

public class OrderBookValuesUpdate(
    IEnumerable<KeyValuePair<decimal, decimal>> AsksUpdates,
    IEnumerable<KeyValuePair<decimal, decimal>> BidsUpdates)
    : EventBase
{
    public override async ValueTask Handle(Ticker ticker)
    {
        ticker.OrderBookManager.UpdateOrderBookValues(AsksUpdates, BidsUpdates);

        await new OrderBookUpdatedEvent().Handle(ticker);
    }
}