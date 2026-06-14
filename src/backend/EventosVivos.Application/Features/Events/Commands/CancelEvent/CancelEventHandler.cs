using EventosVivos.Domain;
using EventosVivos.Domain.Repositories;
using EventosVivos.Domain.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EventosVivos.Application.Features.Events.Commands.CancelEvent;

/// <summary>
/// Request to cancel an event.
/// </summary>
/// <param name="EventId">Identifier of the event to cancel.</param>
public record CancelEventRequest(Guid EventId) : IRequest<Result>;

/// <summary>
/// Handles event cancellation: marks the event as canceled and records the time.
/// </summary>
public class CancelEventHandler : IRequestHandler<CancelEventRequest, Result>
{
    private readonly IEventRepository _eventRepository;
    private readonly IClock _clock;
    private readonly ILogger<CancelEventHandler> _logger;

    public CancelEventHandler(
        IEventRepository eventRepository,
        IClock clock,
        ILogger<CancelEventHandler>? logger = null)
    {
        _eventRepository = eventRepository;
        _clock = clock;
        _logger = logger ?? NullLogger<CancelEventHandler>.Instance;
    }

    public async Task<Result> Handle(CancelEventRequest request, CancellationToken ct = default)
    {
        var @event = await _eventRepository.GetByIdAsync(request.EventId, ct);
        if (@event is null)
            return Result.Failure($"Event with ID {request.EventId} not found.", ErrorType.NotFound);

        try
        {
            @event.Cancel(_clock.UtcNow);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message);
        }

        await _eventRepository.UpdateAsync(@event, ct);
        _logger.LogInformation("Event canceled {EventId}", @event.Id);
        return Result.Success();
    }
}
