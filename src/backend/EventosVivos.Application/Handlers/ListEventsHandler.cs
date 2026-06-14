using EventosVivos.Domain;
using EventosVivos.Domain.Enums;
using EventosVivos.Domain.Policies;
using EventosVivos.Domain.Repositories;
using EventosVivos.Domain.Services;
using EventosVivos.Application.DTOs;

namespace EventosVivos.Application.Handlers;

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
/// <param name="PageSize">Items per page (1-50, default 10).</param>
public record ListEventsQuery(
    string? Type = null,
    DateTimeOffset? StartsAtFrom = null,
    DateTimeOffset? StartsAtTo = null,
    int? VenueId = null,
    string? Status = null,
    string? TitleSearch = null,
    int PageNumber = 1,
    int PageSize = 10);

/// <summary>
/// Handles event listing: queries via repository filters, derives public status,
/// optionally filters by derived status in memory, then paginates with stable ordering.
/// Status derivation is done in memory because it depends on the current clock,
/// so pagination happens after status filtering for correctness.
/// </summary>
public class ListEventsHandler
{
    private readonly IEventRepository _eventRepository;
    private readonly IClock _clock;

    public ListEventsHandler(IEventRepository eventRepository, IClock clock)
    {
        _eventRepository = eventRepository;
        _clock = clock;
    }

    public async Task<Result<PagedResult<EventResponse>>> HandleAsync(
        ListEventsQuery query, CancellationToken ct = default)
    {
        // Validate pagination parameters
        if (query.PageNumber < 1)
            return Result<PagedResult<EventResponse>>.Failure("PageNumber must be 1 or greater.", ErrorType.Validation);
        if (query.PageSize < 1 || query.PageSize > 50)
            return Result<PagedResult<EventResponse>>.Failure("PageSize must be between 1 and 50.", ErrorType.Validation);

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

        // Fetch from repository with structural filters (non-status filters applied at DB level)
        var events = await _eventRepository.GetFilteredAsync(
            type: parsedType,
            venueId: query.VenueId,
            startsAtFrom: query.StartsAtFrom,
            startsAtTo: query.StartsAtTo,
            isCanceled: null, // we derive status, so include all
            titleSearch: query.TitleSearch,
            ct: ct);

        // Derive public status and filter by status if requested (in-memory)
        var filtered = events
            .Select(e => new
            {
                Event = e,
                PublicStatus = EventStatusPolicy.GetPublicStatus(e, _clock)
            })
            .Where(x => !parsedStatus.HasValue || x.PublicStatus == parsedStatus.Value)
            .ToList();

        // Stable ordering: StartsAt asc, Title asc
        filtered = filtered
            .OrderBy(x => x.Event.Schedule.StartsAt)
            .ThenBy(x => x.Event.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Paginate
        var totalCount = filtered.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);
        if (totalPages == 0) totalPages = 1;

        var pagedItems = filtered
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new EventResponse(
                x.Event.Id,
                x.Event.Title,
                x.Event.Description,
                EventTypeApiMapper.ToApiString(x.Event.Type),
                x.Event.VenueId,
                EventStatusApiMapper.ToApiString(x.PublicStatus),
                x.Event.Schedule.StartsAt,
                x.Event.Schedule.EndsAt,
                x.Event.Price.Amount,
                x.Event.MaxCapacity))
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
