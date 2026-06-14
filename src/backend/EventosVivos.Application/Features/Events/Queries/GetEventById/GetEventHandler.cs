using EventosVivos.Domain;
using EventosVivos.Domain.Policies;
using EventosVivos.Domain.Repositories;
using EventosVivos.Domain.Services;
using EventosVivos.Application.DTOs;
using MediatR;

namespace EventosVivos.Application.Features.Events.Queries.GetEventById;

/// <summary>
/// Query to get a single event by ID.
/// </summary>
/// <param name="EventId">Identifier of the event to retrieve.</param>
public record GetEventQuery(Guid EventId) : IRequest<Result<EventResponse>>;

/// <summary>
/// Handles fetching a single event by ID with derived public status.
/// </summary>
public class GetEventHandler : IRequestHandler<GetEventQuery, Result<EventResponse>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IClock _clock;

    public GetEventHandler(IEventRepository eventRepository, IClock clock)
    {
        _eventRepository = eventRepository;
        _clock = clock;
    }

    public async Task<Result<EventResponse>> Handle(
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
