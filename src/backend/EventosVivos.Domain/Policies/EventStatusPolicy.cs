using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Enums;
using EventosVivos.Domain.Services;

namespace EventosVivos.Domain.Policies;

/// <summary>
/// Derives the public-facing event status from persistence state and current time.
/// RN-06: completado derived when now is after event end, unless canceled.
/// Canceled beats completed.
/// </summary>
public static class EventStatusPolicy
{
    public static EventStatus GetPublicStatus(Event @event, IClock clock)
    {
        if (@event.IsCanceled)
            return EventStatus.Cancelado;

        var bogotaNow = ColombiaTime.NowInColombia(clock);
        var bogotaEnd = TimeZoneInfo.ConvertTime(@event.Schedule.EndsAt, ColombiaTime.Info);

        return bogotaNow > bogotaEnd ? EventStatus.Completado : EventStatus.Activo;
    }
}
