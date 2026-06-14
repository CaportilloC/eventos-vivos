using EventosVivos.Domain.Enums;
using EventosVivos.Domain.ValueObjects;

namespace EventosVivos.Domain.Entities;

public class Event
{
    public Guid Id { get; private set; }
    public string Title { get; private set; }
    public string? Description { get; private set; }
    public EventType Type { get; private set; }
    public int VenueId { get; private set; }
    public int MaxCapacity { get; private set; }
    public Money Price { get; private set; }
    public EventSchedule Schedule { get; private set; }
    public bool IsCanceled { get; private set; }
    public DateTimeOffset? CanceledAt { get; private set; }

    private Event() { Title = null!; Price = null!; Schedule = null!; } // EF Core

    public Event(
        string title,
        EventType type,
        int venueId,
        int maxCapacity,
        Money price,
        EventSchedule schedule,
        string? description = null)
    {
        Id = Guid.NewGuid();
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Description = description;
        Type = type;
        VenueId = venueId;
        MaxCapacity = maxCapacity;
        Price = price ?? throw new ArgumentNullException(nameof(price));
        Schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));
        IsCanceled = false;
    }

    public void Cancel(DateTimeOffset canceledAt)
    {
        if (IsCanceled)
            throw new InvalidOperationException("Event is already canceled.");
        IsCanceled = true;
        CanceledAt = canceledAt;
    }

    /// <summary>
    /// Updates mutable fields on an existing event.
    /// Business rules (capacity, overlap, weekend, etc.) are enforced by the caller.
    /// </summary>
    public void Update(
        string title,
        EventType type,
        int venueId,
        int maxCapacity,
        Money price,
        EventSchedule schedule,
        string? description = null)
    {
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Type = type;
        VenueId = venueId;
        MaxCapacity = maxCapacity;
        Price = price ?? throw new ArgumentNullException(nameof(price));
        Schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));
        Description = description;
    }
}
