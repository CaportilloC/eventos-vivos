using EventosVivos.Application.DTOs;
using EventosVivos.Domain;
using EventosVivos.Domain.Policies;
using EventosVivos.Domain.Repositories;
using EventosVivos.Domain.Rules;
using EventosVivos.Domain.Services;
using MediatR;

namespace EventosVivos.Application.Features.Reservations.Queries.ListAvailableReservationEvents;

public record ListAvailableReservationEventsQuery : IRequest<Result<IReadOnlyList<AvailableReservationEventResponse>>>;

public class ListAvailableReservationEventsHandler
    : IRequestHandler<ListAvailableReservationEventsQuery, Result<IReadOnlyList<AvailableReservationEventResponse>>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly IClock _clock;

    public ListAvailableReservationEventsHandler(
        IEventRepository eventRepository,
        IReservationRepository reservationRepository,
        IClock clock)
    {
        _eventRepository = eventRepository;
        _reservationRepository = reservationRepository;
        _clock = clock;
    }

    public async Task<Result<IReadOnlyList<AvailableReservationEventResponse>>> Handle(
        ListAvailableReservationEventsQuery query,
        CancellationToken ct = default)
    {
        var reservationCutoff = _clock.UtcNow.AddHours(ReservationRules.LatestReservationHoursBeforeStart);
        var events = await _eventRepository.GetActiveForReservationAsync(reservationCutoff, ct);
        var responses = new List<AvailableReservationEventResponse>();

        foreach (var @event in events)
        {
            var reservations = await _reservationRepository.GetByEventIdAsync(@event.Id, ct);
            var inventory = ReservationInventorySnapshot.From(@event, reservations, _clock.UtcNow);
            if (inventory.AvailableTickets <= 0)
                continue;

            var status = EventStatusPolicy.GetPublicStatus(@event, _clock);
            responses.Add(new AvailableReservationEventResponse(
                @event.Id,
                @event.Title,
                @event.Schedule.StartsAt,
                @event.Price.Amount,
                @event.MaxCapacity,
                inventory.HeldTickets,
                inventory.AvailableTickets,
                EventStatusApiMapper.ToApiString(status)));
        }

        return Result<IReadOnlyList<AvailableReservationEventResponse>>.Success(responses.AsReadOnly());
    }
}
