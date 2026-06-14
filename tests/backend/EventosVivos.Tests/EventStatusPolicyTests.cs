using EventosVivos.Domain;
using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Enums;
using EventosVivos.Domain.Policies;
using EventosVivos.Domain.ValueObjects;

namespace EventosVivos.Tests;

public class EventStatusPolicyTests
{
    private static readonly Money Price = new(50);
    private static readonly int VenueId = 1;

    private static (Event @event, FakeClock clock) CreateEvent(
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        DateTimeOffset clockNow)
    {
        var schedule = new EventSchedule(startsAt, endsAt);
        var @event = new Event("Test", EventType.Conferencia, VenueId, 100, Price, schedule);
        var clock = new FakeClock(clockNow);
        return (@event, clock);
    }

    // RN-06: Completed status derived when now > event end, unless canceled
    [Fact]
    public void GetPublicStatus_AfterEventEnd_ReturnsCompletado()
    {
        var startsAt = new DateTimeOffset(2026, 6, 10, 14, 0, 0, TimeSpan.Zero);
        var endsAt = new DateTimeOffset(2026, 6, 10, 16, 0, 0, TimeSpan.Zero);
        var now = new DateTimeOffset(2026, 6, 10, 18, 0, 0, TimeSpan.Zero);

        var (@event, clock) = CreateEvent(startsAt, endsAt, now);

        var status = EventStatusPolicy.GetPublicStatus(@event, clock);

        Assert.Equal(EventStatus.Completado, status);
    }

    [Fact]
    public void GetPublicStatus_BeforeEventEnd_ReturnsActivo()
    {
        var startsAt = new DateTimeOffset(2026, 6, 10, 14, 0, 0, TimeSpan.Zero);
        var endsAt = new DateTimeOffset(2026, 6, 10, 16, 0, 0, TimeSpan.Zero);
        var now = new DateTimeOffset(2026, 6, 10, 15, 0, 0, TimeSpan.Zero); // during event

        var (@event, clock) = CreateEvent(startsAt, endsAt, now);

        var status = EventStatusPolicy.GetPublicStatus(@event, clock);

        Assert.Equal(EventStatus.Activo, status);
    }

    [Fact]
    public void GetPublicStatus_AfterEndButCanceled_ReturnsCancelado()
    {
        var startsAt = new DateTimeOffset(2026, 6, 10, 14, 0, 0, TimeSpan.Zero);
        var endsAt = new DateTimeOffset(2026, 6, 10, 16, 0, 0, TimeSpan.Zero);
        var now = new DateTimeOffset(2026, 6, 10, 18, 0, 0, TimeSpan.Zero);

        var (@event, clock) = CreateEvent(startsAt, endsAt, now);
        @event.Cancel(now); // cancel beats completed

        var status = EventStatusPolicy.GetPublicStatus(@event, clock);

        Assert.Equal(EventStatus.Cancelado, status);
    }

    [Fact]
    public void GetPublicStatus_BeforeEndAndCanceled_ReturnsCancelado()
    {
        var startsAt = new DateTimeOffset(2026, 6, 10, 14, 0, 0, TimeSpan.Zero);
        var endsAt = new DateTimeOffset(2026, 6, 10, 16, 0, 0, TimeSpan.Zero);
        var now = new DateTimeOffset(2026, 6, 10, 15, 0, 0, TimeSpan.Zero);

        var (@event, clock) = CreateEvent(startsAt, endsAt, now);
        @event.Cancel(now);

        var status = EventStatusPolicy.GetPublicStatus(@event, clock);

        Assert.Equal(EventStatus.Cancelado, status);
    }

    [Fact]
    public void GetPublicStatus_BeforeEventStart_ReturnsActivo()
    {
        var startsAt = new DateTimeOffset(2026, 6, 10, 14, 0, 0, TimeSpan.Zero);
        var endsAt = new DateTimeOffset(2026, 6, 10, 16, 0, 0, TimeSpan.Zero);
        var now = new DateTimeOffset(2026, 6, 10, 10, 0, 0, TimeSpan.Zero);

        var (@event, clock) = CreateEvent(startsAt, endsAt, now);

        var status = EventStatusPolicy.GetPublicStatus(@event, clock);

        Assert.Equal(EventStatus.Activo, status);
    }

    /// <summary>
    /// Edge case: event ends at exactly the same time as clock now in Colombia.
    /// We check: completado triggers only when bogotaNow > bogotaEnd.
    /// </summary>
    [Fact]
    public void GetPublicStatus_AtExactEndTime_ReturnsActivo()
    {
        var bogotaOffset = TimeSpan.FromHours(-5);
        var endsAt = new DateTimeOffset(2026, 6, 10, 16, 0, 0, bogotaOffset);
        var startsAt = endsAt.AddHours(-2);
        var nowInBogota = endsAt; // same instant

        var (@event, clock) = CreateEvent(startsAt.ToUniversalTime(), endsAt.ToUniversalTime(), nowInBogota.ToUniversalTime());

        var status = EventStatusPolicy.GetPublicStatus(@event, clock);

        Assert.Equal(EventStatus.Activo, status);
    }
}
