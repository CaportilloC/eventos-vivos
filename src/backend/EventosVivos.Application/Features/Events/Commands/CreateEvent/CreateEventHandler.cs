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

namespace EventosVivos.Application.Features.Events.Commands.CreateEvent;

/// <summary>
/// Request to create a new event.
/// </summary>
/// <param name="Title">Event title (5–100 characters).</param>
/// <param name="Description">Event description (10–500 characters).</param>
/// <param name="VenueId">Identifier of the venue where the event takes place.</param>
/// <param name="MaxCapacity">Maximum number of tickets. Cannot exceed the venue's capacity (RN-01).</param>
/// <param name="StartsAt">Event start date/time. Must be in the future.</param>
/// <param name="EndsAt">Event end date/time. Weekend events must end by 22:00 (RN-03).</param>
/// <param name="Price">Ticket price in USD. Must be greater than zero.</param>
/// <param name="Type">Event type: conferencia, taller, or concierto.</param>
public record CreateEventRequest(
    string Title,
    string? Description,
    int VenueId,
    int MaxCapacity,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    decimal Price,
    string Type) : IRequest<Result<Guid>>;

/// <summary>
/// Handles event creation: validates business rules, checks venue capacity,
/// prevents venue overlap (RN-02), and persists the event.
/// </summary>
public class CreateEventHandler : IRequestHandler<CreateEventRequest, Result<Guid>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IVenueRepository _venueRepository;
    private readonly IClock _clock;
    private readonly ITransactionRunner _transactionRunner;
    private readonly ILogger<CreateEventHandler> _logger;

    public CreateEventHandler(
        IEventRepository eventRepository,
        IVenueRepository venueRepository,
        IClock clock,
        ITransactionRunner? transactionRunner = null,
        ILogger<CreateEventHandler>? logger = null)
    {
        _eventRepository = eventRepository;
        _venueRepository = venueRepository;
        _clock = clock;
        _transactionRunner = transactionRunner ?? new NoopTransactionRunner();
        _logger = logger ?? NullLogger<CreateEventHandler>.Instance;
    }

    public async Task<Result<Guid>> Handle(CreateEventRequest request, CancellationToken ct = default)
    {
        // Resolve event type
        if (!EventTypeApiMapper.TryParse(request.Type, out var eventType))
            return Result<Guid>.Failure($"Invalid event type '{request.Type}'. Use conferencia, taller, or concierto.", ErrorType.Validation);

        // Resolve venue
        var venue = await _venueRepository.GetByIdAsync(request.VenueId, ct);
        if (venue is null)
            return Result<Guid>.Failure($"Venue with ID {request.VenueId} not found.", ErrorType.NotFound);

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
            return Result<Guid>.Failure(ex.Message, ErrorType.Validation);
        }

        // RN-01/RN-03: Event creation policy
        var policyResult = EventCreationPolicy.Validate(
            request.Title, request.Description, request.MaxCapacity, venue.Capacity, schedule, _clock);
        if (policyResult.IsFailure)
            return Result<Guid>.Failure(policyResult.Error!, policyResult.ErrorType);

        return await _transactionRunner.RunSerializableAsync(async transactionCt =>
        {
            // RN-02: Keep overlap validation and persistence in one serializable transaction.
            // Interval-overlap constraints are impractical as a simple relational unique constraint.
            var venueEvents = await _eventRepository.GetByVenueIdAsync(request.VenueId, transactionCt);
            var hasOverlap = venueEvents.Any(e =>
                !e.IsCanceled &&
                e.Schedule.StartsAt < schedule.EndsAt &&
                e.Schedule.EndsAt > schedule.StartsAt);
            if (hasOverlap)
                return Result<Guid>.Failure("The venue already has an active event overlapping with this time slot.");

            var @event = new Event(
                request.Title,
                eventType,
                request.VenueId,
                request.MaxCapacity,
                price,
                schedule,
                request.Description);

            await _eventRepository.AddAsync(@event, transactionCt);
            _logger.LogInformation(
                "Event created {EventId} at venue {VenueId} with capacity {MaxCapacity}",
                @event.Id,
                @event.VenueId,
                @event.MaxCapacity);
            return Result<Guid>.Success(@event.Id);
        }, ct);
    }
}
