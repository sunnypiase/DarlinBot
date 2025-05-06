using Darlin.Domain.Models;

namespace Darlin.Domain.Events;

public class OrderBookUpdatedEvent : EventBase
{
    public override async ValueTask Handle(Ticker ticker)
    {
        // 1) Remove outdated order‑blocks
        var toRemove = new List<decimal>();
        foreach (var kv in ticker.OrderBlockManager.OrderBlocks)
        {
            var price = kv.Key;
            var block = kv.Value;
            if (ticker.OrderBookManager.TryGetValue(price, out var level))
            {
                if (level.Volume < ticker.Threshold
                    || block.Side != level.Side)
                    toRemove.Add(price);
            }
            else
            {
                toRemove.Add(price);
            }
        }

        foreach (var price in toRemove)
            ticker.OrderBlockManager.RemoveOrderBlock(price);

        // 2) Add new signals for big volumes
        foreach (var level in ticker.OrderBookManager.AllLevels)
            if (level.Volume >= ticker.Threshold
                && !ticker.OrderBlockManager.OrderBlocks.ContainsKey(level.Price))
            {
                var ob = new OrderBlock
                {
                    Price = level.Price,
                    Volume = level.Volume,
                    Side = level.Side
                };
                ticker.OrderBlockManager.AddOrderBlock(ob);
            }

        // 3) Fire the next stage
        await new OrderBlockUpdatedEvent().Handle(ticker);
    }
}