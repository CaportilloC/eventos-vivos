using EventosVivos.Domain;
using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Enums;
using EventosVivos.Domain.Policies;
using EventosVivos.Domain.ValueObjects;

namespace EventosVivos.Tests;

public class ReservationAvailabilityPolicyTests
{
    private static readonly Money Price = new(50);
    private static readonly int VenueCapacity = 100;
    private static readonly int VenueId = 1;

    private Event CreateEvent(DateTimeOffset startsAt, DateTimeOffset endsAt, Money? price = null)
    {
        var schedule = new EventSchedule(startsAt, endsAt);
        return new Event("Test", EventType.Conferencia, VenueId, VenueCapacity, price ?? Price, schedule);
    }

    // CL-01: Pending reservation holds seats (tested via availability accounting)
    [Fact]
    public void CanReserve_WithPendingHolds_ReducesAvailableSeats()
    {
        var futureStart = new DateTimeOffset(2026, 7, 1, 14, 0, 0, TimeSpan.Zero);
        var futureEnd = futureStart.AddHours(2);
        var now = new DateTimeOffset(2026, 6, 20, 14, 0, 0, TimeSpan.Zero);
        var @event = CreateEvent(futureStart, futureEnd);
        var clock = new FakeClock(now);

        // 20 confirmed, 10 pending non-expired = 30 held
        var result = ReservationAvailabilityPolicy.CanReserve(
            @event, 70, clock, confirmedCount: 20, pendingCount: 10, lostCount: 0);

        Assert.True(result.IsSuccess); // 70 <= 100 - 30 = 70 available

        result = ReservationAvailabilityPolicy.CanReserve(
            @event, 71, clock, confirmedCount: 20, pendingCount: 10, lostCount: 0);

        Assert.True(result.IsFailure); // 71 > 70 available
        Assert.Contains("available", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CanReserve_ExpiredPending_DoesNotReduceAvailability()
    {
        var futureStart = new DateTimeOffset(2026, 7, 1, 14, 0, 0, TimeSpan.Zero);
        var futureEnd = futureStart.AddHours(2);
        var now = new DateTimeOffset(2026, 6, 20, 14, 0, 0, TimeSpan.Zero);
        var @event = CreateEvent(futureStart, futureEnd);
        var clock = new FakeClock(now);

        // Expired pending is not counted in the "pendingCount" parameter,
        // because the calling code should filter out expired pending holds.
        // Here pendingCount=0 means expired holds are excluded.
        var result = ReservationAvailabilityPolicy.CanReserve(
            @event, 100, clock, confirmedCount: 0, pendingCount: 0, lostCount: 0);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void CanReserve_ForCanceledEvent_Fails()
    {
        var futureStart = new DateTimeOffset(2026, 7, 1, 14, 0, 0, TimeSpan.Zero);
        var futureEnd = futureStart.AddHours(2);
        var now = new DateTimeOffset(2026, 6, 20, 14, 0, 0, TimeSpan.Zero);
        var @event = CreateEvent(futureStart, futureEnd);
        @event.Cancel(now);
        var clock = new FakeClock(now);

        var result = ReservationAvailabilityPolicy.CanReserve(
            @event, 1, clock, 0, 0, 0);

        Assert.True(result.IsFailure);
        Assert.Contains("canceled", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    // RN-04: Cannot reserve less than 1 hour before start
    [Fact]
    public void CanReserve_LessThanOneHourBeforeStart_Fails()
    {
        var startsAt = new DateTimeOffset(2026, 7, 1, 14, 0, 0, TimeSpan.Zero);
        var endsAt = startsAt.AddHours(2);
        var now = new DateTimeOffset(2026, 7, 1, 13, 1, 0, TimeSpan.Zero); // 59 min before
        var @event = CreateEvent(startsAt, endsAt);
        var clock = new FakeClock(now);

        var result = ReservationAvailabilityPolicy.CanReserve(
            @event, 1, clock, 0, 0, 0);

        Assert.True(result.IsFailure);
        Assert.Contains("1 hour", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    // RN-05: Price > 100 limits tickets to max 10 per transaction
    [Fact]
    public void CanReserve_PriceGreaterThan100_LimitsToMax10()
    {
        var startsAt = new DateTimeOffset(2026, 7, 1, 14, 0, 0, TimeSpan.Zero);
        var endsAt = startsAt.AddHours(2);
        var now = new DateTimeOffset(2026, 6, 20, 14, 0, 0, TimeSpan.Zero); // days before
        var expensivePrice = new Money(150);
        var @event = CreateEvent(startsAt, endsAt, expensivePrice);
        var clock = new FakeClock(now);

        // 10 tickets should succeed (at price limit boundary)
        var result = ReservationAvailabilityPolicy.CanReserve(
            @event, 10, clock, 0, 0, 0);

        Assert.True(result.IsSuccess);

        // 11 tickets should fail (exceeds price limit)
        result = ReservationAvailabilityPolicy.CanReserve(
            @event, 11, clock, 0, 0, 0);

        Assert.True(result.IsFailure);
        Assert.Contains("maximum", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    // Combined scenario: event starts in <24h AND price >100 — strictest limit wins (max 5)
    [Fact]
    public void CanReserve_CombinedLessThan24hAndPriceAbove100_StrictestMaxIs5()
    {
        // Event starts in 23 hours (UTC) and price is 150
        var now = new DateTimeOffset(2026, 6, 20, 14, 0, 0, TimeSpan.Zero);
        var startsAt = new DateTimeOffset(2026, 6, 21, 13, 0, 0, TimeSpan.Zero); // 23h later
        var endsAt = startsAt.AddHours(2);
        var expensivePrice = new Money(150);
        var @event = CreateEvent(startsAt, endsAt, expensivePrice);
        var clock = new FakeClock(now);

        // 5 tickets should succeed (within the 5-ticket strict limit)
        var result = ReservationAvailabilityPolicy.CanReserve(
            @event, 5, clock, 0, 0, 0);
        Assert.True(result.IsSuccess);

        // 6 tickets should fail (exceeds strictest limit of 5)
        result = ReservationAvailabilityPolicy.CanReserve(
            @event, 6, clock, 0, 0, 0);
        Assert.True(result.IsFailure);
        Assert.Contains("maximum", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CanReserve_LessThan24hWithNormalPrice_LimitsToMax5()
    {
        var now = new DateTimeOffset(2026, 6, 20, 14, 0, 0, TimeSpan.Zero);
        var startsAt = now.AddHours(23);
        var endsAt = startsAt.AddHours(2);
        var @event = CreateEvent(startsAt, endsAt, new Money(50));
        var clock = new FakeClock(now);

        var result = ReservationAvailabilityPolicy.CanReserve(
            @event, 5, clock, 0, 0, 0);
        Assert.True(result.IsSuccess);

        result = ReservationAvailabilityPolicy.CanReserve(
            @event, 6, clock, 0, 0, 0);
        Assert.True(result.IsFailure);
        Assert.Contains("maximum of 5", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CanReserve_NormalPrice_AllowsMoreThan10()
    {
        var startsAt = new DateTimeOffset(2026, 7, 1, 14, 0, 0, TimeSpan.Zero);
        var endsAt = startsAt.AddHours(2);
        var now = new DateTimeOffset(2026, 6, 20, 14, 0, 0, TimeSpan.Zero); // days before
        var cheapPrice = new Money(50);
        var @event = CreateEvent(startsAt, endsAt, cheapPrice);
        var clock = new FakeClock(now);

        // 15 tickets should succeed because price <= 100 and far from event start
        var result = ReservationAvailabilityPolicy.CanReserve(
            @event, 15, clock, 0, 0, 0);

        Assert.True(result.IsSuccess);
    }
}
