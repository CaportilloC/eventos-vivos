using EventosVivos.Domain.Services;

namespace EventosVivos.Infrastructure.Services;

/// <summary>
/// Production clock that returns the actual UTC time.
/// Registered as singleton; injectable via <see cref="IClock"/>.
/// </summary>
public class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
