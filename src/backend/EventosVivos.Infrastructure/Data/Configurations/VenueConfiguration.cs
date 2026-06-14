using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EventosVivos.Domain.Entities;

namespace EventosVivos.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="Venue"/> entity.
/// </summary>
public class VenueConfiguration : IEntityTypeConfiguration<Venue>
{
    public void Configure(EntityTypeBuilder<Venue> builder)
    {
        builder.ToTable("Venues");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).ValueGeneratedNever();

        builder.Property(v => v.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(v => v.Capacity)
            .IsRequired();

        builder.Property(v => v.City)
            .IsRequired()
            .HasMaxLength(100);

        // Seed the three PDF venues with explicit IDs
        builder.HasData(
            new Venue(1, "Auditorio Central", 200, "Bogotá"),
            new Venue(2, "Sala Norte", 50, "Bogotá"),
            new Venue(3, "Arena Sur", 500, "Medellín"));
    }
}
