using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeRace.Domain.Entities;

namespace WeRace.Infrastructure.Data.Configurations;

public class QualifyingConfiguration : IEntityTypeConfiguration<Qualifying>
{
    public void Configure(EntityTypeBuilder<Qualifying> builder)
    {
        builder.ToTable("qualifying");

        builder.HasKey(q => q.Id);

        builder.Property(q => q.Number)
            .IsRequired();

        builder.Property(q => q.Position)
            .IsRequired();

        builder.Property(q => q.Q1)
            .HasMaxLength(255);

        builder.Property(q => q.Q2)
            .HasMaxLength(255);

        builder.Property(q => q.Q3)
            .HasMaxLength(255);

        builder.HasIndex(q => q.RaceId);
        builder.HasIndex(q => new { q.RaceId, q.DriverId });

        builder.HasOne(q => q.Race)
            .WithMany(r => r.Qualifyings)
            .HasForeignKey(q => q.RaceId)
            .IsRequired();

        builder.HasOne(q => q.Driver)
            .WithMany(d => d.Qualifyings)
            .HasForeignKey(q => q.DriverId)
            .IsRequired();

        builder.HasOne(q => q.Constructor)
            .WithMany(c => c.Qualifyings)
            .HasForeignKey(q => q.ConstructorId)
            .IsRequired();
    }
}
