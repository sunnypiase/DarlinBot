using Binance.Net.Clients;

namespace Darlin.DataRetrievers;

public class BinanceOrderBookSnapshotRetriever(ILogger<BinanceOrderBookSnapshotRetriever> logger)
{
    /// <summary>
    ///     Retrieves a fresh snapshot of the order book from Binance COIN‑M Futures using the REST API.
    /// </summary>
    /// <param name="symbol">The symbol (e.g. "BTCUSD_PERP") in the correct format</param>
    /// <param name="limit">The order book depth limit (for example, 1000)</param>
    /// <returns>A tuple of lists containing ask updates and bid updates.</returns>
    public async Task<(List<KeyValuePair<decimal, decimal>> Ask, List<KeyValuePair<decimal, decimal>> Bid)>
        GetOrderBookSnapshot(string symbol, int limit)
    {
        var askUpdates = new List<KeyValuePair<decimal, decimal>>();
        var bidUpdates = new List<KeyValuePair<decimal, decimal>>();
        try
        {
            // Create a new Binance REST client (configured for COIN‑M Futures)
            using (var client = new BinanceRestClient())
            {
                logger.LogInformation($"[CoinFutures OrderBookSnapshot, {symbol}] Requesting order book snapshot...");

                var orderBookResult =
                    await client.UsdFuturesApi.ExchangeData.GetOrderBookAsync(symbol.ToUpper(), limit);
                if (!orderBookResult.Success)
                {
                    logger.LogInformation(
                        $"[CoinFutures OrderBookSnapshot, {symbol}] Error retrieving snapshot: {orderBookResult.Error}");
                    throw new Exception(orderBookResult.Error?.Message);
                }

                var orderBook = orderBookResult.Data;
                if (orderBook != null)
                {
                    foreach (var ask in orderBook.Asks)
                        askUpdates.Add(new KeyValuePair<decimal, decimal>(ask.Price, ask.Quantity));
                    foreach (var bid in orderBook.Bids)
                        bidUpdates.Add(new KeyValuePair<decimal, decimal>(bid.Price, bid.Quantity));
                    logger.LogInformation(
                        $"[CoinFutures OrderBookSnapshot, {symbol}] Snapshot retrieved successfully.");
                }
                else
                {
                    logger.LogInformation($"[CoinFutures OrderBookSnapshot, {symbol}] Received null order book data.");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogInformation($"[CoinFutures OrderBookSnapshot, {symbol}] Exception: {ex.Message}");
            throw;
        }

        return (askUpdates, bidUpdates);
    }
}