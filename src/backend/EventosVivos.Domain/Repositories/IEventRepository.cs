using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Enums;

namespace EventosVivos.Domain.Repositories;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Event>> GetByVenueIdAsync(int venueId, CancellationToken ct = default);
    Task<PagedQueryResult<Event>> GetFilteredPageAsync(
        EventType? type,
        int? venueId,
        DateTimeOffset? startsAtFrom,
        DateTimeOffset? startsAtTo,
        EventStatus? status,
        DateTimeOffset now,
        string? titleSearch,
        int pageNumber,
        int pageSize,
        CancellationToken ct = default);
    Task AddAsync(Event @event, CancellationToken ct = default);
    Task UpdateAsync(Event @event, CancellationToken ct = default);
}
