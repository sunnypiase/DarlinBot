using Binance.Net.Clients;
using Darlin.Logging;
using Serilog;

namespace Darlin.DataRetrievers;

public class BinanceDayTickerStatsRetriever
{
    public async Task<List<string>> GetTopTickersByVolume(int topN)
    {
        Log.Information("{EventId}: Requesting top {TopN} tickers by 24h volume",
            LogEvents.SubscriptionSuccess, topN);

        try
        {
            using var client = new BinanceRestClient();
            var result = await client.UsdFuturesApi.ExchangeData.GetTickersAsync();
            if (!result.Success)
            {
                Log.Warning("{EventId}: Error retrieving tickers: {Error}",
                    LogEvents.SubscriptionRetry, result.Error);
                throw new InvalidOperationException(result.Error?.Message);
            }

            var filtered = result.Data
                .Where(t => t.Symbol.EndsWith("USDT", StringComparison.OrdinalIgnoreCase))
                .Where(t => !Config.Blacklist.Contains(t.Symbol))
                .OrderByDescending(t => t.QuoteVolume)
                .Take(topN)
                .Select(t => t.Symbol)
                .ToList();

            Log.Information("{EventId}: Retrieved top {Count} tickers",
                LogEvents.SubscriptionSuccess, filtered.Count);

            return filtered;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "{EventId}: Exception fetching top tickers",
                LogEvents.SubscriptionRetry);
            throw;
        }
    }
}