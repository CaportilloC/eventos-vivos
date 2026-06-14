using EventosVivos.Domain;
using EventosVivos.Domain.Enums;
using EventosVivos.Domain.Policies;
using EventosVivos.Domain.Repositories;
using EventosVivos.Domain.Services;
using EventosVivos.Application.DTOs;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EventosVivos.Application.Features.Reservations.Commands.CancelReservation;

/// <summary>
/// Request to cancel a reservation.
/// </summary>
/// <param name="ReservationId">Identifier of the reservation to cancel.</param>
public record CancelReservationRequest(Guid ReservationId) : IRequest<Result<ReservationResponse>>;

/// <summary>
/// Handles reservation cancellation:
/// - pendiente_pago reservations become cancelada and release seats.
/// - confirmada reservations canceled 48h or more before the event become cancelada and release seats.
/// - confirmada reservations canceled less than 48h before the event become perdida and do not release seats.
/// - cancelada/perdida reservations are rejected.
/// </summary>
public class CancelReservationHandler : IRequestHandler<CancelReservationRequest, Result<ReservationResponse>>
{
    private readonly IReservationRepository _reservationRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IClock _clock;
    private readonly ILogger<CancelReservationHandler> _logger;

    public CancelReservationHandler(
        IReservationRepository reservationRepository,
        IEventRepository eventRepository,
        IClock clock,
        ILogger<CancelReservationHandler>? logger = null)
    {
        _reservationRepository = reservationRepository;
        _eventRepository = eventRepository;
        _clock = clock;
        _logger = logger ?? NullLogger<CancelReservationHandler>.Instance;
    }

    public async Task<Result<ReservationResponse>> Handle(
        CancelReservationRequest request, CancellationToken ct = default)
    {
        var reservation = await _reservationRepository.GetByIdAsync(request.ReservationId, ct);
        if (reservation is null)
            return Result<ReservationResponse>.Failure(
                $"Reservation with ID {request.ReservationId} not found.", ErrorType.NotFound);

        var now = _clock.UtcNow;

        switch (reservation.Status)
        {
            case ReservationStatus.PendientePago:
            {
                try
                {
                    reservation.CancelPending(now);
                }
                catch (InvalidOperationException ex)
                {
                    return Result<ReservationResponse>.Failure(ex.Message);
                }
                break;
            }

            case ReservationStatus.Confirmada:
            {
                var @event = await _eventRepository.GetByIdAsync(reservation.EventId, ct);
                if (@event is null)
                    return Result<ReservationResponse>.Failure("Associated event not found.", ErrorType.NotFound);

                var (policyResult, releaseSeats) =
                    ReservationCancellationPolicy.CancelConfirmed(reservation, @event, _clock);
                if (policyResult.IsFailure)
                    return Result<ReservationResponse>.Failure(policyResult.Error!);

                try
                {
                    reservation.CancelConfirmed(releaseSeats, now);
                }
                catch (InvalidOperationException ex)
                {
                    return Result<ReservationResponse>.Failure(ex.Message);
                }
                break;
            }

            case ReservationStatus.Cancelada:
                return Result<ReservationResponse>.Failure(
                    "Reservation is already canceled and cannot be canceled again.");

            case ReservationStatus.Perdida:
                return Result<ReservationResponse>.Failure(
                    "Reservation is already lost due to late cancellation and cannot be canceled again.");

            default:
                return Result<ReservationResponse>.Failure(
                    $"Cannot cancel reservation in state {reservation.Status}. Only pending payment or confirmed reservations can be canceled.");
        }

        await _reservationRepository.UpdateAsync(reservation, ct);
        _logger.LogInformation(
            "Reservation canceled {ReservationId} for event {EventId} with status {Status}",
            reservation.Id,
            reservation.EventId,
            reservation.Status);

        return Result<ReservationResponse>.Success(ReservationResponseMapper.FromReservation(reservation));
    }
}
