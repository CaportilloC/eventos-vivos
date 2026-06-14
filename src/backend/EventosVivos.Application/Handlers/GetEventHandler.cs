using EventosVivos.Domain;
using EventosVivos.Domain.Policies;
using EventosVivos.Domain.Repositories;
using EventosVivos.Domain.Services;
using EventosVivos.Application.DTOs;

namespace EventosVivos.Application.Handlers;

/// <summary>
/// Query to get a single event by ID.
/// </summary>
/// <param name="EventId">Identifier of the event to retrieve.</param>
public record GetEventQuery(Guid EventId);

/// <summary>
/// Handles fetching a single event by ID with derived public status.
/// </summary>
public class GetEventHandler
{
    private readonly IEventRepository _eventRepository;
    private readonly IClock _clock;

    public GetEventHandler(IEventRepository eventRepository, IClock clock)
    {
        _eventRepository = eventRepository;
        _clock = clock;
    }

    public async Task<Result<EventResponse>> HandleAsync(
        GetEventQuery query, CancellationToken ct = default)
    {
        var @event = await _eventRepository.GetByIdAsync(query.EventId, ct);
        if (@event is null)
            return Result<EventResponse>.Failure($"Event with ID {query.EventId} not found.", ErrorType.NotFound);

        var publicStatus = EventStatusPolicy.GetPublicStatus(@event, _clock);

        return Result<EventResponse>.Success(new EventResponse(
            @event.Id,
            @event.Title,
            @event.Description,
            EventTypeApiMapper.ToApiString(@event.Type),
            @event.VenueId,
            EventStatusApiMapper.ToApiString(publicStatus),
            @event.Schedule.StartsAt,
            @event.Schedule.EndsAt,
            @event.Price.Amount,
            @event.MaxCapacity));
    }
}
