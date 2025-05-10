using Binance.Net.Clients;
using Darlin;
using Darlin.DataRetrievers;
using Darlin.Domain.Services;
using Darlin.Loggers;
using MongoDB.Driver;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// ─── Load config ─────────────────────────────────────────────────────────────
builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", false, true)
    .AddEnvironmentVariables();

// ─── Read Seq URL (from appsettings.json or overridden by ENV Seq__ServerUrl) ──
var seqUrl = builder.Configuration["Seq:ServerUrl"];

// ─── Serilog bootstrap ───────────────────────────────────────────────────────
var loggerConfig = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration) // picks up Console & File sinks
    .Enrich.FromLogContext();

// only add Seq if a valid URL is present
if (!string.IsNullOrWhiteSpace(seqUrl)) loggerConfig.WriteTo.Seq(seqUrl);

Log.Logger = loggerConfig.CreateLogger();

// ─── Replace default .NET logging with Serilog ───────────────────────────────
builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

// ─── DI registrations ────────────────────────────────────────────────────────
builder.Services.AddSingleton(new BinanceSocketClient());

builder.Services.AddSingleton<BinanceDayTickerStatsRetriever>();
builder.Services.AddSingleton<BinanceExchangeInfoRetriever>();
builder.Services.AddSingleton<BinanceOrderBookSnapshotRetriever>();
builder.Services.AddSingleton<BinanceVolumeRetriever>();

builder.Services.AddSingleton<IMongoClient>(sp =>
    new MongoClient(sp.GetRequiredService<IConfiguration>()
        .GetValue<string>("Mongo:ConnectionString"))
);

// 2) Our logger
builder.Services.AddSingleton<IClosedPositionLogger, MongoClosedPositionLogger>();
builder.Services.AddSingleton<TickerManager>();
builder.Services.AddSingleton<PreInitializationService>();
builder.Services.AddSingleton<InitializationService>();
builder.Services.AddHostedService<Worker>();

// ─── Run ────────────────────────────────────────────────────────────────────
try
{
    Log.Information("▶ Host starting up");
    await builder.Build().RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public static class Config
{
    public static readonly HashSet<string> Blacklist = new()
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