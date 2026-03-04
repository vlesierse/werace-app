using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeRace.Domain.Entities;

namespace WeRace.Infrastructure.Data.Configurations;

public class DriverStandingConfiguration : IEntityTypeConfiguration<DriverStanding>
{
    public void Configure(EntityTypeBuilder<DriverStanding> builder)
    {
        builder.ToTable("driver_standings");

        builder.HasKey(ds => ds.Id);

        builder.Property(ds => ds.Points)
            .HasPrecision(5, 2)
            .HasDefaultValue(0m);

        builder.Property(ds => ds.PositionText)
            .HasMaxLength(255);

        builder.Property(ds => ds.Wins)
            .HasDefaultValue(0);

        builder.HasIndex(ds => ds.RaceId);
        builder.HasIndex(ds => ds.DriverId);
        builder.HasIndex(ds => new { ds.RaceId, ds.DriverId });

        builder.HasOne(ds => ds.Race)
            .WithMany(r => r.DriverStandings)
            .HasForeignKey(ds => ds.RaceId)
            .IsRequired();

        builder.HasOne(ds => ds.Driver)
            .WithMany(d => d.DriverStandings)
            .HasForeignKey(ds => ds.DriverId)
            .IsRequired();
    }
}
