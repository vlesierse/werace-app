using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeRace.Domain.Entities;

namespace WeRace.Infrastructure.Data.Configurations;

public class RaceConfiguration : IEntityTypeConfiguration<Race>
{
    public void Configure(EntityTypeBuilder<Race> builder)
    {
        builder.ToTable("races");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Round)
            .IsRequired();

        builder.Property(r => r.Name)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(r => r.Date)
            .IsRequired();

        builder.HasIndex(r => new { r.SeasonId, r.Round })
            .IsUnique();

        builder.HasIndex(r => r.SeasonId);
        builder.HasIndex(r => r.CircuitId);
        builder.HasIndex(r => r.Date);

        builder.HasOne(r => r.Season)
            .WithMany(s => s.Races)
            .HasForeignKey(r => r.SeasonId)
            .IsRequired();

        builder.HasOne(r => r.Circuit)
            .WithMany(c => c.Races)
            .HasForeignKey(r => r.CircuitId)
            .IsRequired();
    }
}
