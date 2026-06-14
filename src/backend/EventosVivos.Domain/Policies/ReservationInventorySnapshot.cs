using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Enums;

namespace EventosVivos.Domain.Policies;

public sealed class ReservationInventorySnapshot
{
    private ReservationInventorySnapshot(
        int maxCapacity,
        decimal ticketPrice,
        int confirmedTickets,
        int lostTickets,
        int activePendingTickets)
    {
        MaxCapacity = maxCapacity;
        TicketPrice = ticketPrice;
        ConfirmedTickets = confirmedTickets;
        LostTickets = lostTickets;
        ActivePendingTickets = activePendingTickets;
    }

    public int MaxCapacity { get; }
    public decimal TicketPrice { get; }
    public int ConfirmedTickets { get; }
    public int LostTickets { get; }
    public int ActivePendingTickets { get; }
    public int HeldTickets => ConfirmedTickets + LostTickets + ActivePendingTickets;
    public int AvailableTickets => Math.Max(0, MaxCapacity - HeldTickets);
    public decimal OccupancyPercentage => MaxCapacity > 0
        ? (decimal)ConfirmedTickets / MaxCapacity * 100
        : 0;
    public decimal Revenue => TicketPrice * ConfirmedTickets;

    public static ReservationInventorySnapshot From(
        Event @event,
        IEnumerable<Reservation> reservations,
        DateTimeOffset now,
        Guid? excludedReservationId = null)
    {
        var relevantReservations = reservations
            .Where(r => !excludedReservationId.HasValue || r.Id != excludedReservationId.Value)
            .ToList();

        return new ReservationInventorySnapshot(
            @event.MaxCapacity,
            @event.Price.Amount,
            relevantReservations
                .Where(r => r.Status == ReservationStatus.Confirmada)
                .Sum(r => r.Quantity),
            relevantReservations
                .Where(r => r.Status == ReservationStatus.Perdida)
                .Sum(r => r.Quantity),
            relevantReservations
                .Where(r => r.Status == ReservationStatus.PendientePago && !r.IsExpired(now))
                .Sum(r => r.Quantity));
    }
}
