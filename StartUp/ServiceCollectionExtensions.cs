using Binance.Net.Clients;
using Darlin.DataRetrievers;
using Darlin.Domain.Services;
using Darlin.Loggers;
using Darlin.Repositories;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Darlin.StartUp;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMongoClient(this IServiceCollection services)
    {
        services.AddSingleton<IMongoClient>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<MongoSettings>>().Value;
            return new MongoClient(settings.ConnectionString);
        });
        return services;
    }

    public static IServiceCollection AddBinanceClients(this IServiceCollection services)
    {
        services.AddSingleton(new BinanceSocketClient());
        return services;
    }

    public static IServiceCollection AddDataRetrievers(this IServiceCollection services)
    {
        services.AddSingleton<BinanceDayTickerStatsRetriever>();
        services.AddSingleton<BinanceExchangeInfoRetriever>();
        services.AddSingleton<BinanceOrderBookSnapshotRetriever>();
        services.AddSingleton<BinanceVolumeRetriever>();
        return services;
    }

    public static IServiceCollection AddLoggingServices(this IServiceCollection services)
    {
        services.AddSingleton<IClosedPositionLogger, MongoClosedPositionLogger>();
        return services;
    }

    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        services.AddSingleton<TickerManager>();
        services.AddSingleton<PreInitializationService>();
        services.AddSingleton<InitializationService>();
        return services;
    }

    public static IServiceCollection AddHostedServices(this IServiceCollection services)
    {
        services.AddHostedService<Worker>();
        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddSingleton<IClosedPositionsRepository, MongoDbClosedPositionsRepository>();

        return services;
    }
}