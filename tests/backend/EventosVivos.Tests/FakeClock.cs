using EventosVivos.Domain.Services;

namespace EventosVivos.Tests;

public class FakeClock : IClock
{
    public DateTimeOffset UtcNow { get; }

    public FakeClock(DateTimeOffset utcNow)
    {
        UtcNow = utcNow;
    }

    public static FakeClock FromColombia(DateTimeOffset bogotaTime)
    {
        var utc = bogotaTime.ToUniversalTime();
        return new FakeClock(utc);
    }
}
