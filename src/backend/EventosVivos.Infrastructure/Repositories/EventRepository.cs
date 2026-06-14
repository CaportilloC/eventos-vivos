using Microsoft.EntityFrameworkCore;
using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Enums;
using EventosVivos.Domain.Repositories;
using EventosVivos.Infrastructure.Data;

namespace EventosVivos.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IEventRepository"/>.
/// </summary>
public class EventRepository : IEventRepository
{
    private readonly EventosVivosDbContext _db;

    public EventRepository(EventosVivosDbContext db) => _db = db;

    public async Task<Event?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.Events.FindAsync([id], ct);

    public async Task<IReadOnlyList<Event>> GetByVenueIdAsync(int venueId, CancellationToken ct = default) =>
        await _db.Events.AsNoTracking().Where(e => e.VenueId == venueId).ToListAsync(ct);

    public async Task<IReadOnlyList<Event>> GetFilteredAsync(
        EventType? type = null,
        int? venueId = null,
        DateTimeOffset? startsAtFrom = null,
        DateTimeOffset? startsAtTo = null,
        bool? isCanceled = null,
        string? titleSearch = null,
        CancellationToken ct = default)
    {
        var query = _db.Events.AsNoTracking().AsQueryable();

        if (type.HasValue)
            query = query.Where(e => e.Type == type.Value);

        if (venueId.HasValue)
            query = query.Where(e => e.VenueId == venueId.Value);

        if (startsAtFrom.HasValue)
            query = query.Where(e => e.Schedule.StartsAt >= startsAtFrom.Value);

        if (startsAtTo.HasValue)
            query = query.Where(e => e.Schedule.StartsAt <= startsAtTo.Value);

        if (isCanceled.HasValue)
            query = query.Where(e => e.IsCanceled == isCanceled.Value);

        if (!string.IsNullOrWhiteSpace(titleSearch))
            query = query.Where(e => EF.Functions.Like(e.Title, $"%{titleSearch}%"));

        return await query.ToListAsync(ct);
    }

    public async Task AddAsync(Event @event, CancellationToken ct = default)
    {
        _db.Events.Add(@event);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Event @event, CancellationToken ct = default)
    {
        _db.Events.Update(@event);
        await _db.SaveChangesAsync(ct);
    }
}
