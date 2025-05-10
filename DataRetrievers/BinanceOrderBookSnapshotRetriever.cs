using Binance.Net.Clients;
using Darlin.Logging;
using Serilog;

namespace Darlin.DataRetrievers;

public class BinanceOrderBookSnapshotRetriever
{
    public async Task<(List<KeyValuePair<decimal, decimal>> Ask, List<KeyValuePair<decimal, decimal>> Bid)>
        GetOrderBookSnapshot(string symbol, int limit)
    {
        Log.Debug("{EventId}: Requesting order book snapshot for {Symbol}",
            LogEvents.DepthUpdated, symbol);

        try
        {
            using var client = new BinanceRestClient();
            var result = await client.UsdFuturesApi.ExchangeData.GetOrderBookAsync(
                symbol.ToUpper(), limit);

            if (!result.Success || result.Data == null)
            {
                Log.Warning("{EventId}: Snapshot error for {Symbol}: {Error}",
                    LogEvents.SubscriptionRetry, symbol, result.Error?.Message);
                throw new InvalidOperationException(result.Error?.Message);
            }

            var asks = result.Data.Asks.Select(a => new KeyValuePair<decimal, decimal>(a.Price, a.Quantity)).ToList();
            var bids = result.Data.Bids.Select(b => new KeyValuePair<decimal, decimal>(b.Price, b.Quantity)).ToList();

            Log.Information("{EventId}: Snapshot retrieved for {Symbol}: {AskCount} asks, {BidCount} bids",
                LogEvents.SubscriptionSuccess, symbol, asks.Count, bids.Count);

            return (asks, bids);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "{EventId}: Exception fetching snapshot for {Symbol}",
                LogEvents.SubscriptionRetry, symbol);
            throw;
        }
    }
}