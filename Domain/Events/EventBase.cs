using Darlin.Domain.Models;

namespace Darlin.Domain.Events;

public abstract class EventBase
{
    public DateTime EventTime { get; set; } = DateTime.UtcNow;
    public abstract void Handle(Ticker ticker);
}