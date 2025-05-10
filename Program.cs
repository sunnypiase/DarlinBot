using Darlin.Domain.Models;
using Darlin.Repositories;
using Darlin.StartUp;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ─── Load configuration ─────────────────────────────────────────────────────
builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", false, true)
    .AddEnvironmentVariables();

// ─── Bind settings with Options pattern ──────────────────────────────────────
builder.Services.Configure<MongoSettings>(builder.Configuration.GetSection("Mongo"));
builder.Services.Configure<SeqSettings>(builder.Configuration.GetSection("Seq"));

// ─── Configure Serilog as the host logger ────────────────────────────────────
builder.Host.UseSerilog((context, _, loggerConfig) =>
{
    loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext();

    var seqUrl = context.Configuration["Seq:ServerUrl"];
    if (!string.IsNullOrWhiteSpace(seqUrl)) loggerConfig.WriteTo.Seq(seqUrl);
});

// ─── Register dependencies via extension methods ─────────────────────────────
builder.Services
    .AddMongoClient()
    .AddBinanceClients()
    .AddDataRetrievers()
    .AddRepositories()
    .AddLoggingServices()
    .AddDomainServices()
    .AddHostedServices();

// ─── Build and configure HTTP pipeline ───────────────────────────────────────
var app = builder.Build();

// ─── API routes ───────────────────────────────────────────────────────────────
var api = app.MapGroup("/api/v1");

// health-check / test
api.MapGet("/test", () => Results.Ok("Success!"))
    .WithName("GetTest")
    .Produces<string>();

// GET /api/v1/closed-positions
api.MapGet("/closed-positions", (IClosedPositionsRepository repo) =>
        Results.Ok(repo.GetClosedPositions()))
    .WithName("GetClosedPositions")
    .Produces<IEnumerable<ClosedPositionDto>>();

// GET /api/v1/closed-positions/minimal
api.MapGet("/closed-positions/minimal", (IClosedPositionsRepository repo) =>
        Results.Ok(repo.GetClosedPositionsMinimal()))
    .WithName("GetClosedPositionsMinimal")
    .Produces<IEnumerable<ClosedPositionMinimalDto>>();

Log.Information("▶ Host starting up");
await app.RunAsync();