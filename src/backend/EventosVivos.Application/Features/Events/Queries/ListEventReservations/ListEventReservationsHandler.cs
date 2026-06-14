using EventosVivos.Application;
using EventosVivos.Application.DTOs;
using EventosVivos.Application.Features.Reservations.Queries.ListReservations;
using MediatR;

namespace EventosVivos.Application.Features.Events.Queries.ListEventReservations;

public record ListEventReservationsQuery(Guid EventId, int PageNumber = 1, int PageSize = 10)
    : IRequest<Result<PagedResult<ReservationResponse>>>;

public class ListEventReservationsHandler
    : IRequestHandler<ListEventReservationsQuery, Result<PagedResult<ReservationResponse>>>
{
    private readonly ISender _sender;

    public ListEventReservationsHandler(ISender sender)
    {
        _sender = sender;
    }

    public Task<Result<PagedResult<ReservationResponse>>> Handle(
        ListEventReservationsQuery query,
        CancellationToken ct = default) =>
        _sender.Send(new ListReservationsQuery(query.EventId, PageNumber: query.PageNumber, PageSize: query.PageSize), ct);
}
