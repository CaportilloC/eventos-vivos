using EventosVivos.Domain;
using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Enums;
using EventosVivos.Domain.Policies;
using EventosVivos.Domain.Repositories;
using EventosVivos.Domain.Services;
using EventosVivos.Domain.ValueObjects;
using EventosVivos.Application.DTOs;
using EventosVivos.Application.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EventosVivos.Application.Features.Reservations.Commands.UpdateReservation;

/// <summary>
/// Request to update a pending reservation's quantity and buyer info.
/// Only reservations with status <c>pendiente_pago</c> are editable.
/// </summary>
/// <param name="ReservationId">Identifier of the reservation to update.</param>
/// <param name="Quantity">Number of tickets (must be at least 1).</param>
/// <param name="BuyerName">Full name of the ticket buyer (2–100 characters).</param>
/// <param name="BuyerEmail">Email address of the ticket buyer.</param>
public record UpdateReservationRequest(
    Guid ReservationId,
    int Quantity,
    string BuyerName,
    string BuyerEmail) : IRequest<Result<ReservationResponse>>;

/// <summary>
/// Handles reservation updates: validates the reservation is still pending,
/// re-runs availability policy excluding the current reservation's held seats,
/// and persists changes to buyer/quantity.
/// </summary>
public class UpdateReservationHandler : IRequestHandler<UpdateReservationRequest, Result<ReservationResponse>>
{
    private readonly IReservationRepository _reservationRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IClock _clock;
    private readonly ITransactionRunner _transactionRunner;
    private readonly ILogger<UpdateReservationHandler> _logger;

    public UpdateReservationHandler(
        IReservationRepository reservationRepository,
        IEventRepository eventRepository,
        IClock clock,
        ITransactionRunner? transactionRunner = null,
        ILogger<UpdateReservationHandler>? logger = null)
    {
        _reservationRepository = reservationRepository;
        _eventRepository = eventRepository;
        _clock = clock;
        _transactionRunner = transactionRunner ?? new NoopTransactionRunner();
        _logger = logger ?? NullLogger<UpdateReservationHandler>.Instance;
    }

    public async Task<Result<ReservationResponse>> Handle(
        UpdateReservationRequest request, CancellationToken ct = default)
    {
        return await _transactionRunner.RunSerializableAsync(async transactionCt =>
        {
            var reservation = await _reservationRepository.GetByIdAsync(request.ReservationId, transactionCt);
            if (reservation is null)
                return Result<ReservationResponse>.Failure(
                    $"Reservation with ID {request.ReservationId} not found.", ErrorType.NotFound);

            if (reservation.Status != ReservationStatus.PendientePago)
                return Result<ReservationResponse>.Failure(
                    "Only pending-payment reservations can be edited.");

            var now = _clock.UtcNow;
            if (reservation.IsExpired(now))
                return Result<ReservationResponse>.Failure("Expired pending reservations cannot be edited.");

            var @event = await _eventRepository.GetByIdAsync(reservation.EventId, transactionCt);
            if (@event is null)
                return Result<ReservationResponse>.Failure(
                    $"Linked event with ID {reservation.EventId} not found.", ErrorType.NotFound);

            Buyer buyer;
            try
            {
                buyer = new Buyer(request.BuyerName, request.BuyerEmail);
            }
            catch (ArgumentException ex)
            {
                return Result<ReservationResponse>.Failure(ex.Message, ErrorType.Validation);
            }

            if (request.Quantity < 1)
                return Result<ReservationResponse>.Failure("Quantity must be at least 1.", ErrorType.Validation);

            var reservations = await _reservationRepository.GetByEventIdAsync(reservation.EventId, transactionCt);
            var inventory = ReservationInventorySnapshot.From(@event, reservations, now, reservation.Id);

            var policyResult = ReservationAvailabilityPolicy.CanReserve(
                @event,
                request.Quantity,
                _clock,
                inventory.ConfirmedTickets,
                inventory.ActivePendingTickets,
                inventory.LostTickets);
            if (policyResult.IsFailure)
                return Result<ReservationResponse>.Failure(policyResult.Error!, policyResult.ErrorType);

            reservation.Update(buyer, request.Quantity, now);
            await _reservationRepository.UpdateAsync(reservation, transactionCt);
            _logger.LogInformation(
                "Reservation updated {ReservationId} for event {EventId} with quantity {Quantity}",
                reservation.Id,
                reservation.EventId,
                reservation.Quantity);

            return Result<ReservationResponse>.Success(ReservationResponseMapper.FromReservation(reservation));
        }, ct);
    }
}
