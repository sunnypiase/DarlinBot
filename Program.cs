using Binance.Net.Clients;
using Darlin;
using Darlin.DataRetrievers;
using Darlin.Domain.Models;
using Darlin.Domain.Services;
using Darlin.Loggers;

var builder = Host.CreateApplicationBuilder(args);

// Binance client for all subscriptions
builder.Services.AddSingleton(new BinanceSocketClient());

// Data retrievers
builder.Services.AddSingleton<BinanceExchangeInfoRetriever>();
builder.Services.AddSingleton<BinanceOrderBookSnapshotRetriever>();
builder.Services.AddSingleton<BinanceVolumeRetriever>();
builder.Services.AddSingleton<BinanceDayTickerStatsRetriever>();

// Loggers
builder.Services.AddSingleton<OpenPositionFileLogger>(_ => new OpenPositionFileLogger(@"./Data/open_pos.json"));
builder.Services.AddSingleton<CsvLogger<ClosedPositionDTO>>(_ =>
    new CsvLogger<ClosedPositionDTO>(@"./Data/Trades", "ClosedPositions.csv"));

// Core pipeline pieces
builder.Services.AddSingleton<TickerManager>();
builder.Services.AddSingleton<PreInitializationService>();
builder.Services.AddSingleton<InitializationService>();
builder.Services.AddSingleton<StartService>();

// The orchestrator
builder.Services.AddHostedService<Worker>();

await builder.Build().RunAsync();

public static class Config
{
    public static string BinanceRestBaseUri = "https://api.binance.com";
    public static string BinanceWebSocketBaseUri = "wss://stream.binance.com:9443/ws";

    // A blacklist of symbols we do not want to trade.
    public static HashSet<string> Blacklist = new()
    {
        "USDCUSDT", "FDUSDUSDT", "EURUSDT", "WIFUSDT", "VITEUSDT",
        "AMBUSDT", "LITUSDT", "STMXUSDT", "CLVUSDT", "USTCUSDT", "BNXUSDT",
        "XUSDUSDT", "VIDTUSDT", "AGIXUSDT", "LINAUSDT", "FTMUSDT", "WAVESUSDT",
        "OCEANUSDT", "STRAXUSDT", "RENUSDT", "UNFIUSDT", "DGBUSDT", "TROYUSDT",
        "SNTUSDT", "BLZUSDT", "COMBOUSDT", "NULSUSDT", "NOTUSDT", "KEYUSDT", "LOOMUSDT",
        "MDTUSDT", "BONDUSDT", "KLAYUSDT", "XEMUSDT", "OMGUSDT", "REEFUSDT", "RADUSDT",
        "GLMRUSDT", "BADGERUSDT", "CTKUSDT", "DARUSDT",
        "SLPUSDT", "CVXUSDT", "BALUSDT", "ORBSUSDT", "STPTUSDT", "ALPACAUSDT"
    };
}