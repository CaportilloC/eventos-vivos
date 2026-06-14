using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Enums;

namespace EventosVivos.Domain.Repositories;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Event>> GetByVenueIdAsync(int venueId, CancellationToken ct = default);
    Task<IReadOnlyList<Event>> GetFilteredAsync(
        EventType? type = null,
        int? venueId = null,
        DateTimeOffset? startsAtFrom = null,
        DateTimeOffset? startsAtTo = null,
        bool? isCanceled = null,
        string? titleSearch = null,
        CancellationToken ct = default);
    Task AddAsync(Event @event, CancellationToken ct = default);
    Task UpdateAsync(Event @event, CancellationToken ct = default);
}
