using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeRace.Domain.Entities;

namespace WeRace.Infrastructure.Data.Configurations;

public class ResultConfiguration : IEntityTypeConfiguration<Result>
{
    public void Configure(EntityTypeBuilder<Result> builder)
    {
        builder.ToTable("results");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Grid)
            .IsRequired();

        builder.Property(r => r.PositionText)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(r => r.PositionOrder)
            .IsRequired();

        builder.Property(r => r.Points)
            .HasPrecision(5, 2)
            .HasDefaultValue(0m);

        builder.Property(r => r.Laps)
            .HasDefaultValue(0);

        builder.Property(r => r.Time)
            .HasMaxLength(255);

        builder.Property(r => r.FastestLapTime)
            .HasMaxLength(255);

        builder.Property(r => r.FastestLapSpeed)
            .HasMaxLength(255);

        builder.HasIndex(r => r.RaceId);
        builder.HasIndex(r => r.DriverId);
        builder.HasIndex(r => r.ConstructorId);
        builder.HasIndex(r => new { r.RaceId, r.DriverId });

        builder.HasOne(r => r.Race)
            .WithMany(race => race.Results)
            .HasForeignKey(r => r.RaceId)
            .IsRequired();

        builder.HasOne(r => r.Driver)
            .WithMany(d => d.Results)
            .HasForeignKey(r => r.DriverId)
            .IsRequired();

        builder.HasOne(r => r.Constructor)
            .WithMany(c => c.Results)
            .HasForeignKey(r => r.ConstructorId)
            .IsRequired();

        builder.HasOne(r => r.Status)
            .WithMany(s => s.Results)
            .HasForeignKey(r => r.StatusId)
            .IsRequired();
    }
}
