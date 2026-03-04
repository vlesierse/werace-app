using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeRace.Domain.Entities;

namespace WeRace.Infrastructure.Data.Configurations;

public class PitStopConfiguration : IEntityTypeConfiguration<PitStop>
{
    public void Configure(EntityTypeBuilder<PitStop> builder)
    {
        builder.ToTable("pit_stops");

        builder.HasKey(p => new { p.RaceId, p.DriverId, p.Stop });

        builder.Property(p => p.Lap)
            .IsRequired();

        builder.Property(p => p.Duration)
            .HasMaxLength(255);

        builder.HasIndex(p => p.RaceId);
        builder.HasIndex(p => p.DriverId);

        builder.HasOne(p => p.Race)
            .WithMany(r => r.PitStops)
            .HasForeignKey(p => p.RaceId)
            .IsRequired();

        builder.HasOne(p => p.Driver)
            .WithMany(d => d.PitStops)
            .HasForeignKey(p => p.DriverId)
            .IsRequired();
    }
}
