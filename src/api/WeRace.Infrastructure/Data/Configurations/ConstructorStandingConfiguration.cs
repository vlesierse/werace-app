using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeRace.Domain.Entities;

namespace WeRace.Infrastructure.Data.Configurations;

public class ConstructorStandingConfiguration : IEntityTypeConfiguration<ConstructorStanding>
{
    public void Configure(EntityTypeBuilder<ConstructorStanding> builder)
    {
        builder.ToTable("constructor_standings");

        builder.HasKey(cs => cs.Id);

        builder.Property(cs => cs.Points)
            .HasPrecision(5, 2)
            .HasDefaultValue(0m);

        builder.Property(cs => cs.PositionText)
            .HasMaxLength(255);

        builder.Property(cs => cs.Wins)
            .HasDefaultValue(0);

        builder.HasIndex(cs => cs.RaceId);
        builder.HasIndex(cs => cs.ConstructorId);

        builder.HasOne(cs => cs.Race)
            .WithMany(r => r.ConstructorStandings)
            .HasForeignKey(cs => cs.RaceId)
            .IsRequired();

        builder.HasOne(cs => cs.Constructor)
            .WithMany(c => c.ConstructorStandings)
            .HasForeignKey(cs => cs.ConstructorId)
            .IsRequired();
    }
}
