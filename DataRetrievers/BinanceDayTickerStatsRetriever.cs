using Binance.Net.Clients;

namespace Darlin.DataRetrievers;

public class BinanceDayTickerStatsRetriever(ILogger<BinanceDayTickerStatsRetriever> logger)
{
    /// <summary>
    ///     Returns the top N tickers by 24hr volume that contain "USDT" and are not in the blacklist.
    ///     Uses the Binance USD Futures API to retrieve ticker statistics.
    /// </summary>
    /// <param name="topN">The number of top tickers to return.</param>
    /// <returns>A list of ticker symbols.</returns>
    public async Task<List<string>> GetTopTickersByVolume(int topN)
    {
        try
        {
            using (var client = new BinanceRestClient())
            {
                logger.LogInformation(
                    "[USDFutures DayTickerStatsRetriever] Requesting 24hr ticker statistics for all symbols...");

                // Retrieve ticker statistics for all USD Futures symbols.
                // The GetTickerAsync() method (without a symbol parameter) returns a collection of 24h stats.
                var result = await client.UsdFuturesApi.ExchangeData.GetTickersAsync();

                if (!result.Success)
                {
                    logger.LogInformation(
                        $"[USDFutures DayTickerStatsRetriever] Error retrieving ticker stats: {result.Error}");
                    throw new Exception(result.Error?.Message);
                }

                var tickers = result.Data;

                // Filter to include only symbols ending with "USDT" and not in the blacklist,
                // then order by QuoteVolume descending and take the top N.
                var filtered = tickers
                    .Where(t => t.Symbol.EndsWith("USDT", StringComparison.OrdinalIgnoreCase))
                    .Where(t => !Config.Blacklist.Contains(t.Symbol))
                    .OrderByDescending(t => t.QuoteVolume)
                    .Take(topN)
                    .Select(t => t.Symbol)
                    .ToList();

                logger.LogInformation(
                    $"[USDFutures DayTickerStatsRetriever] Successfully retrieved top {topN} tickers.");
                return filtered;
            }
        }
        catch (Exception ex)
        {
            logger.LogInformation($"[USDFutures DayTickerStatsRetriever] Exception: {ex.Message}");
            throw;
        }
    }
}