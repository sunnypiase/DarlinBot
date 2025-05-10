using Darlin.Domain.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Darlin.Loggers;

public class CoinPositions
{
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonElement("coinName")]
    public string CoinName { get; set; }

    [BsonElement("positions")]
    public List<ClosedPositionDto> Positions { get; set; } = [];
}
