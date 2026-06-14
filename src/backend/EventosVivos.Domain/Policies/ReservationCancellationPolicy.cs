using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Rules;
using EventosVivos.Domain.Services;

namespace EventosVivos.Domain.Policies;

/// <summary>
/// Encapsulates the 48-hour cancellation rule (RN-07).
/// Early cancellation (>= 48h before start): cancelada, seats released.
/// Late cancellation (less than 48h before start): perdida, seats NOT released.
/// </summary>
public static class ReservationCancellationPolicy
{
    /// <summary>
    /// Cancels a confirmed reservation applying the 48-hour rule.
    /// Returns true if seats should be released, false if perdida.
    /// </summary>
    public static (Result result, bool releaseSeats) CancelConfirmed(
        Reservation reservation,
        Event @event,
        IClock clock)
    {
        if (reservation.Status != Enums.ReservationStatus.Confirmada)
            return (Result.Failure(
                $"Cannot cancel: reservation is in state {reservation.Status}."), false);

        var hoursUntilEvent = (@event.Schedule.StartsAt - clock.UtcNow).TotalHours;

        if (hoursUntilEvent >= ReservationRules.LateCancellationPenaltyHours)
        {
            // Cancelada, release seats
            return (Result.Success(), true);
        }
        else
        {
            // Perdida, do NOT release seats
            return (Result.Success(), false);
        }
    }
}
