namespace EventosVivos.Domain.ValueObjects;

public record EventSchedule
{
    public DateTimeOffset StartsAt { get; }
    public DateTimeOffset EndsAt { get; }

    public EventSchedule(DateTimeOffset startsAt, DateTimeOffset endsAt)
    {
        if (startsAt >= endsAt)
            throw new ArgumentException("Event end must be after start.");
        StartsAt = startsAt;
        EndsAt = endsAt;
    }

    public TimeSpan Duration => EndsAt - StartsAt;
}
