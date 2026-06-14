using EventosVivos.Domain.Enums;

namespace EventosVivos.Application.DTOs;

public static class EventStatusApiMapper
{
    public static string ToApiString(EventStatus status) => status switch
    {
        EventStatus.Activo => "activo",
        EventStatus.Completado => "completado",
        EventStatus.Cancelado => "cancelado",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };

    public static bool TryParse(string? value, out EventStatus status)
    {
        status = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return value.Trim().ToLowerInvariant() switch
        {
            "activo" => Set(EventStatus.Activo, out status),
            "completado" => Set(EventStatus.Completado, out status),
            "cancelado" => Set(EventStatus.Cancelado, out status),
            _ => Enum.TryParse(value, ignoreCase: true, out status)
        };
    }

    private static bool Set(EventStatus value, out EventStatus status)
    {
        status = value;
        return true;
    }
}
