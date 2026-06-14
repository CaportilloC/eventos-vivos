using EventosVivos.Application.DTOs;
using EventosVivos.Domain;
using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Repositories;
using MediatR;

namespace EventosVivos.Application.Features.Venues.Queries.ListVenues;

public record ListVenuesQuery(int PageNumber = 1, int PageSize = 50) : IRequest<Result<PagedResult<Venue>>>;

public class ListVenuesHandler : IRequestHandler<ListVenuesQuery, Result<PagedResult<Venue>>>
{
    private readonly IVenueRepository _venueRepository;

    public ListVenuesHandler(IVenueRepository venueRepository)
    {
        _venueRepository = venueRepository;
    }

    public async Task<Result<PagedResult<Venue>>> Handle(ListVenuesQuery query, CancellationToken ct = default)
    {
        if (query.PageNumber < 1)
            return Result<PagedResult<Venue>>.Failure("PageNumber must be 1 or greater.", ErrorType.Validation);
        if (query.PageSize < 1 || query.PageSize > 50)
            return Result<PagedResult<Venue>>.Failure("PageSize must be between 1 and 50.", ErrorType.Validation);

        var venues = await _venueRepository.GetAllAsync(ct);
        var ordered = venues.OrderBy(v => v.Id).ToList();
        var totalCount = ordered.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);
        if (totalPages == 0) totalPages = 1;

        var pagedItems = ordered
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList()
            .AsReadOnly();

        return Result<PagedResult<Venue>>.Success(new PagedResult<Venue>(
            pagedItems,
            query.PageNumber,
            query.PageSize,
            totalCount,
            totalPages,
            query.PageNumber > 1,
            query.PageNumber < totalPages));
    }
}
