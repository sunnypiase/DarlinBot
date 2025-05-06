using Darlin.Domain.Models;

namespace Darlin.Domain.Events;

public class TresholdEvent(decimal NewVolume)
    : EventBase
{
    public override async ValueTask Handle(Ticker ticker)
    {
        ticker.UpdateThreshold(NewVolume);
        await new OrderBookUpdatedEvent().Handle(ticker);
    }
}