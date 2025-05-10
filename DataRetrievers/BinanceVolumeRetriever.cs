using Binance.Net.Clients;
using Binance.Net.Enums;

namespace Darlin.DataRetrievers;

public class BinanceVolumeRetriever
{
    /// <summary>
    ///     Retrieves raw 5m volumes for the last 5d (up to 1440 entries)
    /// </summary>
    public async Task<List<decimal>> GetVolumes(string symbol)
    {
        using var client = new BinanceRestClient();
        var end = DateTime.UtcNow;
        var start = end.AddDays(-5);
        var result = await client.UsdFuturesApi.ExchangeData.GetKlinesAsync(
            symbol.ToUpper(),
            KlineInterval.FiveMinutes,
            start,
            end,
            288*5);

        if (!result.Success || result.Data == null)
            throw new Exception(result.Error?.Message ?? "Failed to fetch historical volumes");

        return result.Data.Select(k => k.Volume).ToList();
    }

    internal static decimal CalculateMedian(List<decimal> nums)
    {
        var sorted = nums.OrderBy(x => x).ToList();
        var count = sorted.Count;
        return count % 2 == 0
            ? (sorted[count / 2 - 1] + sorted[count / 2]) / 2
            : sorted[count / 2];
    }

    internal static decimal CalculateStdDev(List<decimal> nums)
    {
        var avg = nums.Average();
        var sumSq = nums.Sum(x => (x - avg) * (x - avg));
        var var = sumSq / nums.Count;
        return (decimal)Math.Sqrt((double)var);
    }
}