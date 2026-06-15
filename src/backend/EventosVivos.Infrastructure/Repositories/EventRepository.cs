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

    public async Task<IReadOnlyList<Event>> GetActiveForReservationAsync(DateTimeOffset reservationCutoff, CancellationToken ct = default) =>
        await _db.Events
            .AsNoTracking()
            .Where(e => !e.IsCanceled && e.Schedule.StartsAt >= reservationCutoff)
            .OrderByDescending(e => e.Schedule.StartsAt)
            .ThenBy(e => e.Title)
            .ToListAsync(ct);

    public async Task<PagedQueryResult<Event>> GetFilteredPageAsync(
        EventType? type,
        int? venueId,
        DateTimeOffset? startsAtFrom,
        DateTimeOffset? startsAtTo,
        EventStatus? status,
        DateTimeOffset now,
        string? titleSearch,
        int pageNumber,
        int pageSize,
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

        if (status.HasValue)
        {
            query = status.Value switch
            {
                EventStatus.Cancelado => query.Where(e => e.IsCanceled),
                EventStatus.Completado => query.Where(e => !e.IsCanceled && e.Schedule.EndsAt < now),
                EventStatus.Activo => query.Where(e => !e.IsCanceled && e.Schedule.EndsAt >= now),
                _ => query
            };
        }

        if (!string.IsNullOrWhiteSpace(titleSearch))
            query = query.Where(e => EF.Functions.Like(e.Title, $"%{titleSearch}%"));

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(e => e.Schedule.StartsAt)
            .ThenBy(e => e.Title)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedQueryResult<Event>(items, totalCount);
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
