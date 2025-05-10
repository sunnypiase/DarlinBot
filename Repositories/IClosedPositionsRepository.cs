using Darlin.Domain.Models;

namespace Darlin.Repositories;

internal interface IClosedPositionsRepository
{
    IEnumerable<ClosedPositionDto> GetClosedPositions();
    IEnumerable<ClosedPositionMinimalDto> GetClosedPositionsMinimal();
}