using Microsoft.EntityFrameworkCore;
using EventosVivos.Domain.Entities;

namespace EventosVivos.Infrastructure.Data;

/// <summary>
/// Application database context for SQL Server via EF Core.
/// Configurations are applied via <see cref="IEntityTypeConfiguration{TEntity}"/>
/// from the same assembly.
/// </summary>
public class EventosVivosDbContext : DbContext
{
    public EventosVivosDbContext(DbContextOptions<EventosVivosDbContext> options)
        : base(options) { }

    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Reservation> Reservations => Set<Reservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EventosVivosDbContext).Assembly);
    }
}
