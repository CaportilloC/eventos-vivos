using EventosVivos.Domain.Entities;

namespace EventosVivos.Application.DTOs;

public static class ReservationResponseMapper
{
    public static ReservationResponse FromReservation(Reservation reservation) => new(
        reservation.Id,
        reservation.EventId,
        reservation.Quantity,
        ReservationStatusApiMapper.ToApiString(reservation.Status),
        reservation.Buyer.Name,
        reservation.Buyer.Email,
        reservation.CreatedAt,
        reservation.ExpiresAt,
        reservation.ConfirmedAt,
        reservation.CanceledAt,
        reservation.Code);
}
