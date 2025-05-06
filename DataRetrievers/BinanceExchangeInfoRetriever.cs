using Binance.Net.Clients;

namespace Darlin.DataRetrievers;

public class BinanceExchangeInfoRetriever(ILogger<BinanceExchangeInfoRetriever> logger)
{
    /// <summary>
    ///     Retrieves the pip size (tick size) for a given symbol using the Binance REST API.
    ///     The pip size is determined by the PRICE_FILTER from the exchange info.
    /// </summary>
    /// <param name="symbol">The symbol (for example, "BTCUSDT") in the correct format.</param>
    /// <returns>The pip size as a decimal.</returns>
    public async Task<decimal> GetPipSize(string symbol)
    {
        try
        {
            using (var client = new BinanceRestClient())
            {
                logger.LogInformation($"[ExchangeInfoRetriever] Requesting exchange info for symbol: {symbol}");

                // Call the built-in REST endpoint to get exchange info.
                var exchangeInfoResult = await client.UsdFuturesApi.ExchangeData.GetExchangeInfoAsync();

                if (!exchangeInfoResult.Success || exchangeInfoResult.Data == null)
                {
                    var errorMessage = exchangeInfoResult.Error?.Message ?? "Unknown error";
                    logger.LogInformation($"[ExchangeInfoRetriever] Error retrieving exchange info: {errorMessage}");
                    throw new Exception(errorMessage);
                }

                // Get the symbol information from the returned data.
                var symbolInfo = exchangeInfoResult.Data.Symbols.FirstOrDefault(x =>
                    x.Name.Equals(symbol, StringComparison.CurrentCultureIgnoreCase));
                if (symbolInfo?.PriceFilter == null || symbolInfo.PriceFilter.TickSize == 0)
                    throw new Exception($"Cannot get pip size for {symbol}: symbol not found.");

                var pipSize = symbolInfo.PriceFilter.TickSize;
                logger.LogInformation($"[ExchangeInfoRetriever] Pip size for {symbol} is {pipSize}");
                return pipSize;
            }
        }
        catch (Exception ex)
        {
            logger.LogInformation($"[ExchangeInfoRetriever] Exception: {ex.Message}");
            throw;
        }
    }
}