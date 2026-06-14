using EventosVivos.Domain;
using EventosVivos.Domain.Repositories;
using EventosVivos.Domain.Services;

namespace EventosVivos.Application.Handlers;

/// <summary>
/// Request to cancel an event.
/// </summary>
/// <param name="EventId">Identifier of the event to cancel.</param>
public record CancelEventRequest(Guid EventId);

/// <summary>
/// Handles event cancellation: marks the event as canceled and records the time.
/// </summary>
public class CancelEventHandler
{
    private readonly IEventRepository _eventRepository;
    private readonly IClock _clock;

    public CancelEventHandler(IEventRepository eventRepository, IClock clock)
    {
        _eventRepository = eventRepository;
        _clock = clock;
    }

    public async Task<Result> HandleAsync(CancelEventRequest request, CancellationToken ct = default)
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
        return Result.Success();
    }
}
