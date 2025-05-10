using Darlin.Domain.Models;
using Darlin.Loggers;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Darlin.Repositories;

public class MongoDbClosedPositionsRepository : IClosedPositionsRepository
{
    private readonly IMongoCollection<CoinPositions> _collection;

    public MongoDbClosedPositionsRepository(IMongoClient client, IConfiguration cfg)
    {
        var dbName = cfg["Mongo:Database"] ?? "DarlinDb";
        var db = client.GetDatabase(dbName);
        _collection = db.GetCollection<CoinPositions>("coinPositions");
    }

    public IEnumerable<ClosedPositionDto> GetClosedPositions()
    {
        return _collection
            .Aggregate()
            .Unwind<CoinPositions, ClosedPositionUnwound>(c => c.Positions)
            .ReplaceRoot(u => u.Positions) // ← typed lambda
            .ToEnumerable();
    }

    public IEnumerable<ClosedPositionMinimalDto> GetClosedPositionsMinimal()
    {
        return _collection
            .Aggregate()
            .Unwind<CoinPositions, ClosedPositionUnwound>(c => c.Positions)
            .ReplaceRoot(u => u.Positions) // ← typed lambda
            .Project(dto => new ClosedPositionMinimalDto
            {
                TickerName = dto.TickerName,
                TickerBidPrice = dto.TickerBidPrice,
                TickerAskPrice = dto.TickerAskPrice,
                PipSize = dto.PipSize,
                OrderBlockPrice = dto.OrderBlockPrice,
                OrderBlockVolume = dto.OrderBlockVolume,
                OrderBlockVolumeOnOpen = dto.OrderBlockVolumeOnOpen,
                TresholdOnOpen = dto.TresholdOnOpen,
                MedianOnOpen = dto.MedianOnOpen,
                StdDevOnOpen = dto.StdDevOnOpen,
                OrderBlockSide = dto.OrderBlockSide,
                OrderBlockCreationTime = dto.OrderBlockCreationTime,
                OrderBlockLifeTimeOnOpen = dto.OrderBlockLifeTimeOnOpen,
                OpenPositionOpenPrice = dto.OpenPositionOpenPrice,
                OpenPositionOpenTime = dto.OpenPositionOpenTime,
                TakeProfit = dto.TakeProfit,
                MaxProfitPrice = dto.MaxProfitPrice,
                StopLoss = dto.StopLoss,
                PositionSize = dto.PositionSize,
                ClosedPrice = dto.ClosedPrice,
                PnL = dto.PnL,
                MaxPotentialPnl = dto.MaxPotentialPnl,
                CloseTime = dto.CloseTime,
                CloseReason = dto.CloseReason
            })
            .ToEnumerable();
    }


    [BsonIgnoreExtraElements] // ignore the extra _id & coinName
    public class ClosedPositionUnwound
    {
        [BsonElement("positions")] public ClosedPositionDto Positions { get; set; } = null!;
    }
}