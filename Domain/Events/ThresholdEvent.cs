using Darlin.Domain.Models;

namespace Darlin.Domain.Events;

public class TresholdEvent(decimal NewVolume)
    : EventBase
{
    public override void Handle(Ticker ticker)
    {
        ticker.UpdateThreshold(NewVolume);
        new OrderBookUpdatedEvent().Handle(ticker);
    }
}