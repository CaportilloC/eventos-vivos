using EventosVivos.Domain;
using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Policies;
using EventosVivos.Domain.Repositories;
using EventosVivos.Domain.Services;
using EventosVivos.Domain.ValueObjects;
using EventosVivos.Application.DTOs;
using EventosVivos.Application.Abstractions;

namespace EventosVivos.Application.Handlers;

/// <summary>
/// Request to reserve tickets for an event.
/// </summary>
/// <param name="EventId">Identifier of the event to reserve tickets for.</param>
/// <param name="Quantity">Number of tickets to reserve. Must be at least 1.</param>
/// <param name="BuyerName">Full name of the ticket buyer (2–100 characters).</param>
/// <param name="BuyerEmail">Email address of the ticket buyer.</param>
public record ReserveTicketsRequest(
    Guid EventId,
    int Quantity,
    string BuyerName,
    string BuyerEmail);

/// <summary>
/// Handles ticket reservation: validates availability, buyer info,
/// checks time/price limits, and creates a pending reservation.
/// </summary>
public class ReserveTicketsHandler
{
    private readonly IEventRepository _eventRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly IClock _clock;
    private readonly ITransactionRunner _transactionRunner;

    public ReserveTicketsHandler(
        IEventRepository eventRepository,
        IReservationRepository reservationRepository,
        IClock clock,
        ITransactionRunner? transactionRunner = null)
    {
        _eventRepository = eventRepository;
        _reservationRepository = reservationRepository;
        _clock = clock;
        _transactionRunner = transactionRunner ?? new NoopTransactionRunner();
    }

    public async Task<Result<ReservationResponse>> HandleAsync(
        ReserveTicketsRequest request, CancellationToken ct = default)
    {
        // Build buyer value object
        Buyer buyer;
        try
        {
            buyer = new Buyer(request.BuyerName, request.BuyerEmail);
        }
        catch (ArgumentException ex)
        {
            return Result<ReservationResponse>.Failure(ex.Message, ErrorType.Validation);
        }

        return await _transactionRunner.RunSerializableAsync(async transactionCt =>
        {
            var @event = await _eventRepository.GetByIdAsync(request.EventId, transactionCt);
            if (@event is null)
                return Result<ReservationResponse>.Failure($"Event with ID {request.EventId} not found.", ErrorType.NotFound);

            var reservations = await _reservationRepository.GetByEventIdAsync(request.EventId, transactionCt);
            var now = _clock.UtcNow;
            var inventory = ReservationInventorySnapshot.From(@event, reservations, now);

            var policyResult = ReservationAvailabilityPolicy.CanReserve(
                @event,
                request.Quantity,
                _clock,
                inventory.ConfirmedTickets,
                inventory.ActivePendingTickets,
                inventory.LostTickets);
            if (policyResult.IsFailure)
                return Result<ReservationResponse>.Failure(policyResult.Error!, policyResult.ErrorType);

            var reservation = new Reservation(@event.Id, buyer, request.Quantity, now);
            await _reservationRepository.AddAsync(reservation, transactionCt);

            return Result<ReservationResponse>.Success(Map(reservation));
        }, ct);
    }

    private static ReservationResponse Map(Reservation r) => ReservationResponseMapper.FromReservation(r);
}
