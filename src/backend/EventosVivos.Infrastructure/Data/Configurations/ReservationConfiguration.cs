using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EventosVivos.Domain.Entities;

namespace EventosVivos.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="Reservation"/> entity.
/// Maps owned Buyer value object as in-table columns.
/// </summary>
public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("Reservations");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.EventId)
            .IsRequired();

        builder.Property(r => r.Quantity)
            .IsRequired();

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        builder.Property(r => r.ExpiresAt)
            .IsRequired();

        builder.Property(r => r.ConfirmedAt);

        builder.Property(r => r.CanceledAt);

        builder.Property(r => r.Code)
            .HasMaxLength(10);

        // Store enum as human-readable string
        builder.Property(r => r.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        // Owned value object: Buyer → BuyerName / BuyerEmail columns
        builder.OwnsOne(r => r.Buyer, buyer =>
        {
            buyer.Property(b => b.Name)
                .HasColumnName("BuyerName")
                .IsRequired()
                .HasMaxLength(100);

            buyer.Property(b => b.Email)
                .HasColumnName("BuyerEmail")
                .IsRequired()
                .HasMaxLength(200);
        });

        // Foreign key: Reservation → Event (no navigation property needed)
        builder.HasOne<Event>()
            .WithMany()
            .HasForeignKey(r => r.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for filtering and lookups
        builder.HasIndex(r => r.EventId);
        builder.HasIndex(r => new { r.EventId, r.Status, r.ExpiresAt })
            .HasDatabaseName("IX_Reservations_EventId_Status_ExpiresAt");
        builder.HasIndex(r => r.Status);

        // Unique filtered index on Code (allows multiple nulls)
        builder.HasIndex(r => r.Code)
            .IsUnique()
            .HasDatabaseName("IX_Reservations_Code")
            .HasFilter("[Code] IS NOT NULL");
    }
}
