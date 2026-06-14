using EventosVivos.Domain;
using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Enums;
using EventosVivos.Domain.Policies;
using EventosVivos.Domain.Repositories;
using EventosVivos.Domain.Services;
using EventosVivos.Domain.ValueObjects;
using EventosVivos.Application.Abstractions;
using EventosVivos.Application.DTOs;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EventosVivos.Application.Features.Events.Commands.UpdateEvent;

/// <summary>
/// Request to update an existing event.
/// Only mutable fields are included. Rejects updates on canceled or completed events.
/// </summary>
/// <param name="EventId">Identifier of the event to update.</param>
/// <param name="Title">Event title (5–100 characters).</param>
/// <param name="Description">Event description (10–500 characters).</param>
/// <param name="VenueId">Identifier of the venue.</param>
/// <param name="MaxCapacity">Maximum tickets. Cannot exceed venue capacity (RN-01).</param>
/// <param name="StartsAt">Event start date/time.</param>
/// <param name="EndsAt">Event end date/time. Weekend events must end by 22:00 (RN-03).</param>
/// <param name="Price">Ticket price in COP.</param>
/// <param name="Type">Event type: conferencia, taller, or concierto.</param>
public record UpdateEventRequest(
    Guid EventId,
    string Title,
    string? Description,
    int VenueId,
    int MaxCapacity,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    decimal Price,
    string Type) : IRequest<Result>;

/// <summary>
/// Handles event updates: validates business rules, checks venue capacity,
/// prevents venue overlap excluding current event (RN-02), and persists changes.
/// Rejects updates on canceled or completed events.
/// </summary>
public class UpdateEventHandler : IRequestHandler<UpdateEventRequest, Result>
{
    private readonly IEventRepository _eventRepository;
    private readonly IVenueRepository _venueRepository;
    private readonly IClock _clock;
    private readonly ITransactionRunner _transactionRunner;
    private readonly ILogger<UpdateEventHandler> _logger;

    public UpdateEventHandler(
        IEventRepository eventRepository,
        IVenueRepository venueRepository,
        IClock clock,
        ITransactionRunner? transactionRunner = null,
        ILogger<UpdateEventHandler>? logger = null)
    {
        _eventRepository = eventRepository;
        _venueRepository = venueRepository;
        _clock = clock;
        _transactionRunner = transactionRunner ?? new NoopTransactionRunner();
        _logger = logger ?? NullLogger<UpdateEventHandler>.Instance;
    }

    public async Task<Result> Handle(UpdateEventRequest request, CancellationToken ct = default)
    {
        // Load existing event
        var @event = await _eventRepository.GetByIdAsync(request.EventId, ct);
        if (@event is null)
            return Result.Failure($"Event with ID {request.EventId} not found.", ErrorType.NotFound);

        // Reject updates on canceled or completed events
        if (@event.IsCanceled)
            return Result.Failure("Cannot update a canceled event.");

        var publicStatus = EventStatusPolicy.GetPublicStatus(@event, _clock);
        if (publicStatus == EventStatus.Completado)
            return Result.Failure("Cannot update a completed event.");

        // Resolve event type
        if (!EventTypeApiMapper.TryParse(request.Type, out var eventType))
            return Result.Failure($"Invalid event type '{request.Type}'. Use conferencia, taller, or concierto.", ErrorType.Validation);

        // Resolve venue
        var venue = await _venueRepository.GetByIdAsync(request.VenueId, ct);
        if (venue is null)
            return Result.Failure($"Venue with ID {request.VenueId} not found.", ErrorType.NotFound);

        // Build value objects
        Money price;
        EventSchedule schedule;
        try
        {
            price = new Money(request.Price);
            schedule = new EventSchedule(request.StartsAt, request.EndsAt);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(ex.Message, ErrorType.Validation);
        }

        // RN-01/RN-03: Event creation policy (same rules apply to updates)
        var policyResult = EventCreationPolicy.Validate(
            request.Title, request.Description, request.MaxCapacity, venue.Capacity, schedule, _clock);
        if (policyResult.IsFailure)
            return Result.Failure(policyResult.Error!, policyResult.ErrorType);

        return await _transactionRunner.RunSerializableAsync(async transactionCt =>
        {
            // RN-02: Keep overlap validation and persistence in one serializable transaction.
            // Interval-overlap constraints are impractical as a simple relational unique constraint.
            var venueEvents = await _eventRepository.GetByVenueIdAsync(request.VenueId, transactionCt);
            var hasOverlap = venueEvents.Any(e =>
                e.Id != request.EventId &&
                !e.IsCanceled &&
                e.Schedule.StartsAt < schedule.EndsAt &&
                e.Schedule.EndsAt > schedule.StartsAt);
            if (hasOverlap)
                return Result.Failure("The venue already has an active event overlapping with this time slot.");

            @event.Update(
                request.Title,
                eventType,
                request.VenueId,
                request.MaxCapacity,
                price,
                schedule,
                request.Description);

            await _eventRepository.UpdateAsync(@event, transactionCt);
            _logger.LogInformation(
                "Event updated {EventId} at venue {VenueId} with capacity {MaxCapacity}",
                @event.Id,
                @event.VenueId,
                @event.MaxCapacity);
            return Result.Success();
        }, ct);
    }
}
