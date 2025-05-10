using Darlin.Domain.Models;

namespace Darlin.Loggers;

public interface IClosedPositionLogger
{
    void Log(ClosedPositionDto dto);
}