using Microsoft.EntityFrameworkCore;
using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Repositories;
using EventosVivos.Infrastructure.Data;

namespace EventosVivos.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IVenueRepository"/>.
/// </summary>
public class VenueRepository : IVenueRepository
{
    private readonly EventosVivosDbContext _db;

    public VenueRepository(EventosVivosDbContext db) => _db = db;

    public async Task<Venue?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await _db.Venues.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id, ct);

    public async Task<IReadOnlyList<Venue>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Venues.AsNoTracking().ToListAsync(ct);
}
