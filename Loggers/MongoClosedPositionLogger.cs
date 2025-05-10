using Darlin.Domain.Models;
using MongoDB.Driver;

namespace Darlin.Loggers;

public class MongoClosedPositionLogger : IClosedPositionLogger
{
    private readonly IMongoCollection<CoinPositions> _collection;

    public MongoClosedPositionLogger(IMongoClient client, IConfiguration cfg)
    {
        var dbName   = cfg["Mongo:Database"] ?? "DarlinDb";
        var db       = client.GetDatabase(dbName);
        _collection = db.GetCollection<CoinPositions>("coinPositions");
    }

    public void Log(ClosedPositionDto dto)
    {
        var filter = Builders<CoinPositions>.Filter.Eq(c => c.CoinName, dto.TickerName);
        var update = Builders<CoinPositions>.Update.Push(c => c.Positions, dto);

        _collection.UpdateOne(
            filter,
            update,
            new UpdateOptions { IsUpsert = true }
        );
    }
}