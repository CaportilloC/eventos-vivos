using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Services;
using EventosVivos.Domain.ValueObjects;

namespace EventosVivos.Domain.Policies;

/// <summary>
/// Validates reservation creation: event not canceled, within time limits, price limits,
/// and availability accounting.
/// </summary>
public static class ReservationAvailabilityPolicy
{
    public static Result CanReserve(
        Event @event,
        int quantity,
        IClock clock,
        int confirmedCount,
        int pendingCount,
        int lostCount)
    {
        if (@event.IsCanceled)
            return Result.Failure("Cannot reserve for a canceled event.");

        if (quantity < 1)
            return Result.Failure("Quantity must be at least 1.", ErrorType.Validation);

        // RN-04: cannot reserve less than 1 hour before start
        var bogotaNow = ColombiaTime.NowInColombia(clock);
        var bogotaStart = TimeZoneInfo.ConvertTime(@event.Schedule.StartsAt, ColombiaTime.Info);
        var hoursUntilStart = (bogotaStart - bogotaNow).TotalHours;

        if (hoursUntilStart < 1)
            return Result.Failure(
                "Cannot reserve: event starts in less than 1 hour.");

        // Less than 24 hours: max 5
        var maxByTime = hoursUntilStart < 24 ? 5 : int.MaxValue;

        // RN-05: price > 100: max 10
        var maxByPrice = @event.Price.Amount > 100 ? 10 : int.MaxValue;

        // Strictest limit wins
        var effectiveMax = Math.Min(maxByTime, maxByPrice);
        if (quantity > effectiveMax)
            return Result.Failure(
                $"Quantity {quantity} exceeds the maximum of {effectiveMax} for this event.");

        // Capacity check: confirmed + non-expired pending + lost must not exceed maxCapacity
        var heldSeats = confirmedCount + pendingCount + lostCount;
        var availableSeats = @event.MaxCapacity - heldSeats;

        if (quantity > availableSeats)
            return Result.Failure(
                $"Not enough available seats. Requested {quantity}, available {availableSeats}.");

        return Result.Success();
    }
}
