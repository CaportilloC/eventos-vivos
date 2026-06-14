using EventosVivos.Domain.Enums;
using EventosVivos.Domain.ValueObjects;

namespace EventosVivos.Domain.Entities;

public class Reservation
{
    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public Buyer Buyer { get; private set; }
    public int Quantity { get; private set; }
    public ReservationStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? ConfirmedAt { get; private set; }
    public DateTimeOffset? CanceledAt { get; private set; }
    public string? Code { get; private set; }

    private Reservation() { Buyer = null!; } // EF Core

    public Reservation(Guid eventId, Buyer buyer, int quantity, DateTimeOffset now)
    {
        if (quantity < 1)
            throw new ArgumentException("Quantity must be at least 1.", nameof(quantity));

        Id = Guid.NewGuid();
        EventId = eventId;
        Buyer = buyer ?? throw new ArgumentNullException(nameof(buyer));
        Quantity = quantity;
        Status = ReservationStatus.PendientePago;
        CreatedAt = now;
        ExpiresAt = now.AddMinutes(15);
    }

    public bool IsExpired(DateTimeOffset now) =>
        Status == ReservationStatus.PendientePago && now >= ExpiresAt;

    public bool IsActive => Status is ReservationStatus.PendientePago or ReservationStatus.Confirmada;

    public void Confirm(string code, DateTimeOffset now)
    {
        if (Status != ReservationStatus.PendientePago)
            throw new InvalidOperationException(
                $"Cannot confirm reservation in state {Status}.");
        if (IsExpired(now))
            throw new InvalidOperationException("Expired pending reservations cannot be confirmed.");
        Status = ReservationStatus.Confirmada;
        ConfirmedAt = now;
        Code = code ?? throw new ArgumentNullException(nameof(code));
    }

    /// <summary>
    /// Cancel a confirmed reservation as a pure state machine.
    /// The business rule (48-hour threshold, RN-07) is decided by
    /// <see cref="Policies.ReservationCancellationPolicy"/> —
    /// this method only mutates state based on the given decision.
    /// Returns true if seats should be released, false if perdida.
    /// </summary>
    public bool CancelConfirmed(bool releaseSeats, DateTimeOffset now)
    {
        if (Status != ReservationStatus.Confirmada)
            throw new InvalidOperationException(
                $"Cannot cancel confirmed reservation in state {Status}.");

        CanceledAt = now;
        Status = releaseSeats ? ReservationStatus.Cancelada : ReservationStatus.Perdida;
        return releaseSeats;
    }

    public void CancelPending(DateTimeOffset now)
    {
        if (Status != ReservationStatus.PendientePago)
            throw new InvalidOperationException(
                $"Cannot cancel pending reservation in state {Status}.");
        Status = ReservationStatus.Cancelada;
        CanceledAt = now;
    }

    /// <summary>
    /// Updates mutable fields on a pending reservation.
    /// Business rules (availability, limits) are enforced by the caller.
    /// Only reservations with status <see cref="ReservationStatus.PendientePago"/> may be updated.
    /// </summary>
    public void Update(Buyer buyer, int quantity, DateTimeOffset now)
    {
        if (Status != ReservationStatus.PendientePago)
            throw new InvalidOperationException(
                $"Cannot update reservation in state {Status}.");
        if (IsExpired(now))
            throw new InvalidOperationException("Expired pending reservations cannot be edited.");
        if (quantity < 1)
            throw new ArgumentException("Quantity must be at least 1.", nameof(quantity));

        Buyer = buyer ?? throw new ArgumentNullException(nameof(buyer));
        Quantity = quantity;
    }
}
