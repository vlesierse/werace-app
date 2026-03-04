using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeRace.Domain.Entities;

namespace WeRace.Infrastructure.Data.Configurations;

public class SprintResultConfiguration : IEntityTypeConfiguration<SprintResult>
{
    public void Configure(EntityTypeBuilder<SprintResult> builder)
    {
        builder.ToTable("sprint_results");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Grid)
            .IsRequired();

        builder.Property(s => s.PositionText)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(s => s.PositionOrder)
            .IsRequired();

        builder.Property(s => s.Points)
            .HasPrecision(5, 2)
            .HasDefaultValue(0m);

        builder.Property(s => s.Laps)
            .HasDefaultValue(0);

        builder.Property(s => s.Time)
            .HasMaxLength(255);

        builder.Property(s => s.FastestLapTime)
            .HasMaxLength(255);

        builder.HasIndex(s => s.RaceId);
        builder.HasIndex(s => s.DriverId);

        builder.HasOne(s => s.Race)
            .WithMany(r => r.SprintResults)
            .HasForeignKey(s => s.RaceId)
            .IsRequired();

        builder.HasOne(s => s.Driver)
            .WithMany(d => d.SprintResults)
            .HasForeignKey(s => s.DriverId)
            .IsRequired();

        builder.HasOne(s => s.Constructor)
            .WithMany(c => c.SprintResults)
            .HasForeignKey(s => s.ConstructorId)
            .IsRequired();

        builder.HasOne(s => s.Status)
            .WithMany(st => st.SprintResults)
            .HasForeignKey(s => s.StatusId)
            .IsRequired();
    }
}
