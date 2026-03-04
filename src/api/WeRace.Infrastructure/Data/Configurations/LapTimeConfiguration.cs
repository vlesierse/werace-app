using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeRace.Domain.Entities;

namespace WeRace.Infrastructure.Data.Configurations;

public class LapTimeConfiguration : IEntityTypeConfiguration<LapTime>
{
    public void Configure(EntityTypeBuilder<LapTime> builder)
    {
        builder.ToTable("lap_times");

        builder.HasKey(l => new { l.RaceId, l.DriverId, l.Lap });

        builder.Property(l => l.Time)
            .HasMaxLength(255);

        builder.HasIndex(l => l.RaceId);
        builder.HasIndex(l => l.DriverId);

        builder.HasOne(l => l.Race)
            .WithMany(r => r.LapTimes)
            .HasForeignKey(l => l.RaceId)
            .IsRequired();

        builder.HasOne(l => l.Driver)
            .WithMany(d => d.LapTimes)
            .HasForeignKey(l => l.DriverId)
            .IsRequired();
    }
}
