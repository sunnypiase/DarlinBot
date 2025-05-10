using Binance.Net.Clients;
using Darlin.Logging;
using Serilog;

namespace Darlin.DataRetrievers;

public class BinanceExchangeInfoRetriever
{
    public async Task<decimal> GetPipSize(string symbol)
    {
        Log.Debug("{EventId}: Fetching pip size for {Symbol}",
            LogEvents.DepthUpdated, symbol);

        try
        {
            using var client = new BinanceRestClient();
            var res = await client.UsdFuturesApi.ExchangeData.GetExchangeInfoAsync();
            if (!res.Success || res.Data == null)
            {
                Log.Warning("{EventId}: ExchangeInfo error for {Symbol}: {Error}",
                    LogEvents.SubscriptionRetry, symbol, res.Error?.Message);
                throw new InvalidOperationException(res.Error?.Message);
            }

            var info = res.Data.Symbols
                .FirstOrDefault(x => x.Name.Equals(symbol, StringComparison.OrdinalIgnoreCase));
            if (info?.PriceFilter?.TickSize is not { } tickSize || tickSize == 0)
                throw new KeyNotFoundException($"TickSize missing for {symbol}");

            Log.Information("{EventId}: PipSize for {Symbol} is {PipSize}",
                LogEvents.SubscriptionSuccess, symbol, tickSize);

            return tickSize;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "{EventId}: Exception in GetPipSize for {Symbol}",
                LogEvents.SubscriptionRetry, symbol);
            throw;
        }
    }
}