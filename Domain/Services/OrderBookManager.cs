using Darlin.Domain.Enums;
using Darlin.Domain.Models;

namespace Darlin.Domain.Services;

public class OrderBookManager
{
    private readonly SortedDictionary<decimal, OrderBookElement> _asks = new();
    private readonly SortedDictionary<decimal, OrderBookElement> _bids = new();
    private readonly object _lock = new();

    public OrderBookManager(
        IEnumerable<KeyValuePair<decimal, decimal>> initialAsks,
        IEnumerable<KeyValuePair<decimal, decimal>> initialBids)
    {
        // Bulk‑load snapshot
        foreach (var kv in initialAsks)
            _asks[kv.Key] = new OrderBookElement(kv.Key, kv.Value, OrderBookSide.Ask);
        foreach (var kv in initialBids)
            _bids[kv.Key] = new OrderBookElement(kv.Key, kv.Value, OrderBookSide.Bid);
    }

    /// <summary>All current levels, across asks and bids.</summary>
    public IEnumerable<OrderBookElement> AllLevels
    {
        get
        {
            lock (_lock)
            {
                // Note: Concat is deferred, but we hold the lock for thread safety.
                return _asks.Values.Concat(_bids.Values);
            }
        }
    }

    /// <summary>Total number of levels.</summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _asks.Count + _bids.Count;
            }
        }
    }

    /// <summary>Best (lowest) ask, or null if none.</summary>
    public OrderBookElement? BestAsk
    {
        get
        {
            lock (_lock)
            {
                using var it = _asks.GetEnumerator();
                return it.MoveNext() ? it.Current.Value : null;
            }
        }
    }

    /// <summary>Best (highest) bid, or null if none.</summary>
    public OrderBookElement? BestBid
    {
        get
        {
            lock (_lock)
            {
                if (_bids.Count == 0) return null;
                var lastKey = _bids.Keys.Last();
                return _bids[lastKey];
            }
        }
    }

    /// <summary>
    ///     Try to fetch a level by its price (either side).
    /// </summary>
    public bool TryGetValue(decimal price, out OrderBookElement element)
    {
        lock (_lock)
        {
            if (_asks.TryGetValue(price, out element)) return true;
            if (_bids.TryGetValue(price, out element)) return true;
            return false;
        }
    }

    /// <summary>
    ///     Incremental volume updates. Zero or negative => remove the level.
    /// </summary>
    public void UpdateOrderBookValues(
        IEnumerable<KeyValuePair<decimal, decimal>> askUpdates,
        IEnumerable<KeyValuePair<decimal, decimal>> bidUpdates)
    {
        lock (_lock)
        {
            foreach (var kv in askUpdates)
                if (kv.Value <= 0) _asks.Remove(kv.Key);
                else _asks[kv.Key] = new OrderBookElement(kv.Key, kv.Value, OrderBookSide.Ask);

            foreach (var kv in bidUpdates)
                if (kv.Value <= 0) _bids.Remove(kv.Key);
                else _bids[kv.Key] = new OrderBookElement(kv.Key, kv.Value, OrderBookSide.Bid);
        }
    }

    /// <summary>
    ///     Prune away stale levels outside [bidPrice, askPrice].
    /// </summary>
    public void UpdateOrderBookPrices(decimal askPrice, decimal bidPrice)
    {
        lock (_lock)
        {
            // Remove asks priced _below_ askPrice
            var toRemoveAsks = _asks.Keys.TakeWhile(p => p < askPrice).ToList();
            foreach (var p in toRemoveAsks)
                _asks.Remove(p);

            // Remove bids priced _above_ bidPrice
            var toRemoveBids = _bids.Keys.Reverse().TakeWhile(p => p > bidPrice).ToList();
            foreach (var p in toRemoveBids)
                _bids.Remove(p);
        }
    }
}