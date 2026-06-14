namespace EventosVivos.Application.DTOs;

/// <summary>
/// Public representation of a reservation.
/// </summary>
/// <param name="Id">Reservation unique identifier.</param>
/// <param name="EventId">Identifier of the associated event.</param>
/// <param name="Quantity">Number of tickets reserved.</param>
/// <param name="Status">Reservation status: pendiente_pago, confirmada, cancelada, or perdida.
/// <c>pendiente_pago</c> = awaiting payment confirmation;
/// <c>confirmada</c> = payment confirmed, EV code assigned;
/// <c>cancelada</c> = canceled with seat release (pending) or timely cancel (confirmed with 48h+);
/// <c>perdida</c> = late-canceled confirmed reservation (less than 48h before event), seats NOT released.</param>
/// <param name="BuyerName">Full name of the ticket buyer.</param>
/// <param name="BuyerEmail">Email address of the ticket buyer.</param>
/// <param name="CreatedAt">Timestamp when the reservation was created.</param>
/// <param name="ExpiresAt">Timestamp when a pending reservation expires (if applicable).</param>
/// <param name="ConfirmedAt">Timestamp of payment confirmation (null if not yet confirmed).</param>
/// <param name="CanceledAt">Timestamp of cancellation (null if not canceled).</param>
/// <param name="Code">Unique EV code in format EV-{6 digits}, generated only on payment confirmation.
/// Null while the reservation is pending payment.</param>
public record ReservationResponse(
    Guid Id,
    Guid EventId,
    int Quantity,
    string Status,
    string BuyerName,
    string BuyerEmail,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? ConfirmedAt,
    DateTimeOffset? CanceledAt,
    string? Code);
