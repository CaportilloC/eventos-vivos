using EventosVivos.Domain;
using EventosVivos.Domain.Policies;
using EventosVivos.Domain.Repositories;
using EventosVivos.Domain.Services;
using EventosVivos.Application.DTOs;

namespace EventosVivos.Application.Handlers;

/// <summary>
/// Request to confirm payment for a pending reservation.
/// </summary>
/// <param name="ReservationId">Identifier of the reservation to confirm payment for.</param>
public record ConfirmPaymentRequest(Guid ReservationId);

/// <summary>
/// Handles payment confirmation: transitions reservation from pendiente_pago
/// to confirmada, assigns a unique confirmation code.
/// </summary>
public class ConfirmPaymentHandler
{
    private readonly IReservationRepository _reservationRepository;
    private readonly IClock _clock;

    public ConfirmPaymentHandler(
        IReservationRepository reservationRepository,
        IClock clock)
    {
        _reservationRepository = reservationRepository;
        _clock = clock;
    }

    public async Task<Result<ReservationResponse>> HandleAsync(
        ConfirmPaymentRequest request, CancellationToken ct = default)
    {
        var reservation = await _reservationRepository.GetByIdAsync(request.ReservationId, ct);
        if (reservation is null)
            return Result<ReservationResponse>.Failure(
                $"Reservation with ID {request.ReservationId} not found.", ErrorType.NotFound);

        var now = _clock.UtcNow;
        if (reservation.IsExpired(now))
            return Result<ReservationResponse>.Failure("Expired pending reservations cannot be confirmed.");

        try
        {
            var code = await ReservationCodeGenerator.GenerateUniqueAsync(
                _reservationRepository.CodeExistsAsync, ct);
            reservation.Confirm(code, now);
        }
        catch (InvalidOperationException ex)
        {
            return Result<ReservationResponse>.Failure(ex.Message);
        }

        await _reservationRepository.UpdateAsync(reservation, ct);

        return Result<ReservationResponse>.Success(ReservationResponseMapper.FromReservation(reservation));
    }
}
