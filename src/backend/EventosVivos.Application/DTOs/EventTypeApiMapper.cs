using EventosVivos.Domain.Enums;

namespace EventosVivos.Application.DTOs;

public static class EventTypeApiMapper
{
    public static string ToApiString(EventType type) => type switch
    {
        EventType.Conferencia => "conferencia",
        EventType.Taller => "taller",
        EventType.Concierto => "concierto",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    public static bool TryParse(string? value, out EventType type)
    {
        type = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return value.Trim().ToLowerInvariant() switch
        {
            "conferencia" => Set(EventType.Conferencia, out type),
            "taller" => Set(EventType.Taller, out type),
            "concierto" => Set(EventType.Concierto, out type),
            _ => Enum.TryParse(value, ignoreCase: true, out type)
        };
    }

    private static bool Set(EventType value, out EventType type)
    {
        type = value;
        return true;
    }
}
