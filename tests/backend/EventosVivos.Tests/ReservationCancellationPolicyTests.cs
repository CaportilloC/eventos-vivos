using EventosVivos.Domain;
using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Enums;
using EventosVivos.Domain.Policies;
using EventosVivos.Domain.ValueObjects;

namespace EventosVivos.Tests;

public class ReservationCancellationPolicyTests
{
    private static readonly Buyer ValidBuyer = new("John Doe", "john@example.com");
    private static readonly Money Price = new(50);
    private static readonly int VenueId = 1;

    private static (Event @event, Reservation reservation) Setup(
        DateTimeOffset eventStart,
        DateTimeOffset eventEnd,
        DateTimeOffset reservationNow,
        DateTimeOffset cancellationNow)
    {
        var schedule = new EventSchedule(eventStart, eventEnd);
        var @event = new Event("Test", EventType.Concierto, VenueId, 100, Price, schedule);
        var reservation = new Reservation(@event.Id, ValidBuyer, 2, reservationNow);
        reservation.Confirm(ReservationCodeGenerator.Generate(), reservationNow);
        return (@event, reservation);
    }

    // RN-07: Early confirmed cancellation >= 48h before start releases seats
    [Fact]
    public void CancelConfirmed_GreaterThanOrEqual48HoursBefore_ReleasesSeats()
    {
        var now = new DateTimeOffset(2026, 6, 10, 14, 0, 0, TimeSpan.Zero);
        var eventStart = now.AddHours(48);
        var eventEnd = eventStart.AddHours(2);
        var clock = new FakeClock(now);

        var (@event, reservation) = Setup(eventStart, eventEnd, now.AddDays(-1), now);

        var (result, releaseSeats) =
            ReservationCancellationPolicy.CancelConfirmed(reservation, @event, clock);

        Assert.True(result.IsSuccess);
        Assert.True(releaseSeats);
    }

    [Fact]
    public void CancelConfirmed_Exactly48HoursBefore_ReleasesSeats()
    {
        var now = new DateTimeOffset(2026, 6, 12, 14, 0, 0, TimeSpan.Zero);
        var eventStart = new DateTimeOffset(2026, 6, 14, 14, 0, 0, TimeSpan.Zero);
        var eventEnd = eventStart.AddHours(2);
        var clock = new FakeClock(now);

        var (@event, reservation) = Setup(eventStart, eventEnd, now.AddDays(-1), now);

        var (result, releaseSeats) =
            ReservationCancellationPolicy.CancelConfirmed(reservation, @event, clock);

        Assert.True(result.IsSuccess);
        Assert.True(releaseSeats);
    }

    // RN-07: Late confirmed cancellation < 48h before start is perdida, seats not released
    [Fact]
    public void CancelConfirmed_LessThan48HoursBefore_IsPerdida()
    {
        var now = new DateTimeOffset(2026, 6, 13, 14, 0, 0, TimeSpan.Zero);
        var eventStart = new DateTimeOffset(2026, 6, 14, 14, 0, 0, TimeSpan.Zero); // 24h away
        var eventEnd = eventStart.AddHours(2);
        var clock = new FakeClock(now);

        var (@event, reservation) = Setup(eventStart, eventEnd, now.AddDays(-2), now);

        var (result, releaseSeats) =
            ReservationCancellationPolicy.CancelConfirmed(reservation, @event, clock);

        Assert.True(result.IsSuccess);
        Assert.False(releaseSeats);
    }

    [Fact]
    public void CancelConfirmed_OneHourBefore_IsPerdida()
    {
        var now = new DateTimeOffset(2026, 6, 14, 13, 0, 0, TimeSpan.Zero);
        var eventStart = new DateTimeOffset(2026, 6, 14, 14, 0, 0, TimeSpan.Zero);
        var eventEnd = eventStart.AddHours(2);
        var clock = new FakeClock(now);

        var (@event, reservation) = Setup(eventStart, eventEnd, now.AddDays(-3), now);

        var (result, releaseSeats) =
            ReservationCancellationPolicy.CancelConfirmed(reservation, @event, clock);

        Assert.True(result.IsSuccess);
        Assert.False(releaseSeats);
    }

    [Fact]
    public void CancelConfirmed_EdgeCase47h59m_IsPerdida()
    {
        var now = new DateTimeOffset(2026, 6, 12, 14, 0, 1, TimeSpan.Zero); // 47h59m59s before
        var eventStart = new DateTimeOffset(2026, 6, 14, 14, 0, 0, TimeSpan.Zero);
        var eventEnd = eventStart.AddHours(2);
        var clock = new FakeClock(now);

        var (@event, reservation) = Setup(eventStart, eventEnd, now.AddDays(-2), now);

        var (result, releaseSeats) =
            ReservationCancellationPolicy.CancelConfirmed(reservation, @event, clock);

        Assert.True(result.IsSuccess);
        Assert.False(releaseSeats);
    }
}
