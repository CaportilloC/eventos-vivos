using EventosVivos.Domain;
using EventosVivos.Domain.Policies;
using EventosVivos.Domain.Repositories;
using EventosVivos.Domain.Services;
using EventosVivos.Application.DTOs;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EventosVivos.Application.Features.Reservations.Commands.ConfirmPayment;

/// <summary>
/// Request to confirm payment for a pending reservation.
/// </summary>
/// <param name="ReservationId">Identifier of the reservation to confirm payment for.</param>
public record ConfirmPaymentRequest(Guid ReservationId) : IRequest<Result<ReservationResponse>>;

/// <summary>
/// Handles payment confirmation: transitions reservation from pendiente_pago
/// to confirmada, assigns a unique confirmation code.
/// </summary>
public class ConfirmPaymentHandler : IRequestHandler<ConfirmPaymentRequest, Result<ReservationResponse>>
{
    private readonly IReservationRepository _reservationRepository;
    private readonly IClock _clock;
    private readonly ILogger<ConfirmPaymentHandler> _logger;

    public ConfirmPaymentHandler(
        IReservationRepository reservationRepository,
        IClock clock,
        ILogger<ConfirmPaymentHandler>? logger = null)
    {
        _reservationRepository = reservationRepository;
        _clock = clock;
        _logger = logger ?? NullLogger<ConfirmPaymentHandler>.Instance;
    }

    public async Task<Result<ReservationResponse>> Handle(
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
        _logger.LogInformation(
            "Reservation payment confirmed {ReservationId} for event {EventId}",
            reservation.Id,
            reservation.EventId);

        return Result<ReservationResponse>.Success(ReservationResponseMapper.FromReservation(reservation));
    }
}
