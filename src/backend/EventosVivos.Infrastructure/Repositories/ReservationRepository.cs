using Microsoft.EntityFrameworkCore;
using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Enums;
using EventosVivos.Domain.Repositories;
using EventosVivos.Infrastructure.Data;

namespace EventosVivos.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IReservationRepository"/>.
/// </summary>
public class ReservationRepository : IReservationRepository
{
    private readonly EventosVivosDbContext _db;

    public ReservationRepository(EventosVivosDbContext db) => _db = db;

    public async Task<Reservation?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.Reservations.FindAsync([id], ct);

    public async Task<IReadOnlyList<Reservation>> GetByEventIdAsync(Guid eventId, CancellationToken ct = default) =>
        await _db.Reservations.AsNoTracking().Where(r => r.EventId == eventId).ToListAsync(ct);

    public async Task<PagedQueryResult<Reservation>> GetFilteredPageAsync(
        Guid? eventId,
        ReservationStatus? status,
        string? buyerEmail,
        int pageNumber,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _db.Reservations.AsNoTracking().AsQueryable();

        if (eventId.HasValue)
            query = query.Where(r => r.EventId == eventId.Value);

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(buyerEmail))
            query = query.Where(r => EF.Functions.Like(r.Buyer.Email, $"%{buyerEmail}%"));

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedQueryResult<Reservation>(items, totalCount);
    }

    public async Task<bool> CodeExistsAsync(string code, CancellationToken ct = default) =>
        await _db.Reservations.AnyAsync(r => r.Code == code, ct);

    public async Task AddAsync(Reservation reservation, CancellationToken ct = default)
    {
        _db.Reservations.Add(reservation);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Reservation reservation, CancellationToken ct = default)
    {
        _db.Reservations.Update(reservation);
        await _db.SaveChangesAsync(ct);
    }
}
