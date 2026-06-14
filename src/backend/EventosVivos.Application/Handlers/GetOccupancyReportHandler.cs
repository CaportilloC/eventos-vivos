using EventosVivos.Domain;
using EventosVivos.Domain.Policies;
using EventosVivos.Domain.Repositories;
using EventosVivos.Domain.Services;
using EventosVivos.Application.DTOs;

namespace EventosVivos.Application.Handlers;

/// <summary>
/// Query to get the occupancy report for a specific event.
/// </summary>
/// <param name="EventId">Identifier of the event to get the occupancy report for.</param>
public record GetOccupancyReportQuery(Guid EventId);

/// <summary>
/// Handles occupancy report generation: computes confirmed tickets, lost tickets,
/// available seats, occupancy percentage, and revenue.
/// </summary>
public class GetOccupancyReportHandler
{
    private readonly IEventRepository _eventRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly IClock _clock;

    public GetOccupancyReportHandler(
        IEventRepository eventRepository,
        IReservationRepository reservationRepository,
        IClock clock)
    {
        _eventRepository = eventRepository;
        _reservationRepository = reservationRepository;
        _clock = clock;
    }

    public async Task<Result<OccupancyReportResponse>> HandleAsync(
        GetOccupancyReportQuery query, CancellationToken ct = default)
    {
        var @event = await _eventRepository.GetByIdAsync(query.EventId, ct);
        if (@event is null)
            return Result<OccupancyReportResponse>.Failure(
                $"Event with ID {query.EventId} not found.", ErrorType.NotFound);

        var status = EventStatusPolicy.GetPublicStatus(@event, _clock);

        var reservations = await _reservationRepository.GetByEventIdAsync(query.EventId, ct);
        var now = _clock.UtcNow;
        var inventory = ReservationInventorySnapshot.From(@event, reservations, now);

        return Result<OccupancyReportResponse>.Success(new OccupancyReportResponse(
            query.EventId,
            EventStatusApiMapper.ToApiString(status),
            inventory.ConfirmedTickets,
            inventory.LostTickets,
            inventory.AvailableTickets,
            Math.Round(inventory.OccupancyPercentage, 1),
            inventory.Revenue));
    }
}
