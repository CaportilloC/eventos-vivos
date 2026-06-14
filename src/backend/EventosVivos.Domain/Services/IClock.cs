namespace EventosVivos.Domain.Services;

/// <summary>
/// Injectable clock abstraction for testable time-dependent business rules.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

/// <summary>
/// Colombia timezone helper (America/Bogota).
/// Provides fallback for minimal Docker images without tzdata.
/// </summary>
public static class ColombiaTime
{
    private static TimeZoneInfo? _instance;
    private static readonly object _lock = new();

    public static TimeZoneInfo Info
    {
        get
        {
            if (_instance is not null)
                return _instance;

            lock (_lock)
            {
                if (_instance is not null)
                    return _instance;

                try
                {
                    _instance = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
                }
                catch (TimeZoneNotFoundException)
                {
                    // Fallback for Docker minimal images without tzdata.
                    // America/Bogota is UTC-05 year-round (no DST).
                    _instance = TimeZoneInfo.CreateCustomTimeZone(
                        "America/Bogota",
                        TimeSpan.FromHours(-5),
                        "America/Bogota",
                        "America/Bogota");
                }

                return _instance;
            }
        }
    }

    public static DateTimeOffset NowInColombia(IClock clock) =>
        TimeZoneInfo.ConvertTime(clock.UtcNow, Info);
}
