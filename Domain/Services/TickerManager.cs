using System.Collections.Concurrent;
using Darlin.Domain.Models;

namespace Darlin.Domain.Services;

public class TickerManager
{
    public List<Ticker> Tickers { get; private set; } = new();

    public ConcurrentDictionary<string, Ticker> TickerMap { get; private set; }
        = new();

    public void SetTickers(
        List<Ticker> tickers,
        ConcurrentDictionary<string, Ticker> tickerMap)
    {
        Tickers = tickers;
        TickerMap = tickerMap;
    }
}