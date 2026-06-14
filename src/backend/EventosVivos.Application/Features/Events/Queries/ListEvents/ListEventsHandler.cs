using EventosVivos.Domain;
using EventosVivos.Domain.Enums;
using EventosVivos.Domain.Policies;
using EventosVivos.Domain.Repositories;
using EventosVivos.Domain.Rules;
using EventosVivos.Domain.Services;
using EventosVivos.Application.DTOs;
using MediatR;

namespace EventosVivos.Application.Features.Events.Queries.ListEvents;

/// <summary>
/// Query to list events with optional filters, derived status, and pagination.
/// </summary>
/// <param name="Type">Filter by event type (conferencia, taller, concierto).</param>
/// <param name="StartsAtFrom">Filter events starting at or after this date.</param>
/// <param name="StartsAtTo">Filter events starting at or before this date.</param>
/// <param name="VenueId">Filter by venue ID.</param>
/// <param name="Status">Filter by derived status (activo, completado, cancelado).</param>
/// <param name="TitleSearch">Search by title (partial match).</param>
/// <param name="PageNumber">Page number (1-based, default 1).</param>
/// <param name="PageSize">Items per page (1-100, default 10).</param>
public record ListEventsQuery(
    string? Type = null,
    DateTimeOffset? StartsAtFrom = null,
    DateTimeOffset? StartsAtTo = null,
    int? VenueId = null,
    string? Status = null,
    string? TitleSearch = null,
    int PageNumber = PaginationRules.DefaultPageNumber,
    int PageSize = PaginationRules.DefaultPageSize) : IRequest<Result<PagedResult<EventResponse>>>;

/// <summary>
/// Handles event listing: queries via repository filters, derives public status,
/// optionally filters by derived status in memory, then paginates with stable ordering.
/// Status derivation is done in memory because it depends on the current clock,
/// so pagination happens after status filtering for correctness.
/// </summary>
public class ListEventsHandler : IRequestHandler<ListEventsQuery, Result<PagedResult<EventResponse>>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IClock _clock;

    public ListEventsHandler(IEventRepository eventRepository, IClock clock)
    {
        _eventRepository = eventRepository;
        _clock = clock;
    }

    public async Task<Result<PagedResult<EventResponse>>> Handle(
        ListEventsQuery query, CancellationToken ct = default)
    {
        // Validate pagination parameters
        if (query.PageNumber < 1)
            return Result<PagedResult<EventResponse>>.Failure("PageNumber must be 1 or greater.", ErrorType.Validation);
        if (query.PageSize < 1 || query.PageSize > PaginationRules.MaxPageSize)
            return Result<PagedResult<EventResponse>>.Failure("PageSize must be between 1 and 100.", ErrorType.Validation);

        // Parse type filter
        EventType? parsedType = null;
        if (query.Type is not null)
        {
            if (!EventTypeApiMapper.TryParse(query.Type, out var t))
                return Result<PagedResult<EventResponse>>.Failure(
                    $"Invalid event type '{query.Type}'. Use conferencia, taller, or concierto.", ErrorType.Validation);
            parsedType = t;
        }

        EventStatus? parsedStatus = null;
        if (query.Status is not null)
        {
            if (!EventStatusApiMapper.TryParse(query.Status, out var s))
                return Result<PagedResult<EventResponse>>.Failure(
                    $"Invalid event status '{query.Status}'. Use activo, completado, or cancelado.", ErrorType.Validation);
            parsedStatus = s;
        }

        var page = await _eventRepository.GetFilteredPageAsync(
            type: parsedType,
            venueId: query.VenueId,
            startsAtFrom: query.StartsAtFrom,
            startsAtTo: query.StartsAtTo,
            status: parsedStatus,
            now: _clock.UtcNow,
            titleSearch: query.TitleSearch,
            pageNumber: query.PageNumber,
            pageSize: query.PageSize,
            ct: ct);

        var totalCount = page.TotalCount;
        var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);
        if (totalPages == 0) totalPages = 1;

        var pagedItems = page.Items
            .Select(e => new EventResponse(
                e.Id,
                e.Title,
                e.Description,
                EventTypeApiMapper.ToApiString(e.Type),
                e.VenueId,
                EventStatusApiMapper.ToApiString(EventStatusPolicy.GetPublicStatus(e, _clock)),
                e.Schedule.StartsAt,
                e.Schedule.EndsAt,
                e.Price.Amount,
                e.MaxCapacity))
            .ToList()
            .AsReadOnly();

        return Result<PagedResult<EventResponse>>.Success(new PagedResult<EventResponse>(
            pagedItems,
            query.PageNumber,
            query.PageSize,
            totalCount,
            totalPages,
            query.PageNumber > 1,
            query.PageNumber < totalPages));
    }
}
