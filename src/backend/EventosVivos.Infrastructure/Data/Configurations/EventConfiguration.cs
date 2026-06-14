using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Enums;

namespace EventosVivos.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="Event"/> entity.
/// Maps owned value-objects (Money, EventSchedule) as in-table columns.
/// </summary>
public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("Events");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.VenueId)
            .IsRequired();

        builder.Property(e => e.MaxCapacity)
            .IsRequired();

        builder.Property(e => e.IsCanceled);

        builder.Property(e => e.CanceledAt);

        // Store enum as human-readable string
        builder.Property(e => e.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        // Owned value object: Money → single Price column
        builder.OwnsOne(e => e.Price, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("Price")
                .HasColumnType("decimal(18,2)")
                .IsRequired();
        });

        // Owned value object: EventSchedule → StartsAt / EndsAt columns
        builder.OwnsOne(e => e.Schedule, schedule =>
        {
            schedule.Property(s => s.StartsAt)
                .HasColumnName("StartsAt")
                .IsRequired();

            schedule.Property(s => s.EndsAt)
                .HasColumnName("EndsAt")
                .IsRequired();
        });

        // Foreign key: Event → Venue (no navigation property needed)
        builder.HasOne<Venue>()
            .WithMany()
            .HasForeignKey(e => e.VenueId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for filtering and lookups
        builder.HasIndex(e => e.VenueId);
        builder.HasIndex(e => e.Title).HasDatabaseName("IX_Events_Title");
        builder.HasIndex(e => e.Type);
        builder.HasIndex(e => e.IsCanceled);
        builder.HasIndex(e => new { e.VenueId, e.IsCanceled });
    }
}
