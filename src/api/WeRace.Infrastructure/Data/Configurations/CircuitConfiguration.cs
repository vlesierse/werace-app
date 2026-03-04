using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeRace.Domain.Entities;

namespace WeRace.Infrastructure.Data.Configurations;

public class CircuitConfiguration : IEntityTypeConfiguration<Circuit>
{
    public void Configure(EntityTypeBuilder<Circuit> builder)
    {
        builder.ToTable("circuits");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.CircuitRef)
            .HasMaxLength(255)
            .IsRequired();

        builder.HasIndex(c => c.CircuitRef)
            .IsUnique();

        builder.Property(c => c.Name)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(c => c.Location)
            .HasMaxLength(255);

        builder.Property(c => c.Country)
            .HasMaxLength(255);

        builder.Property(c => c.Latitude)
            .HasPrecision(10, 6);

        builder.Property(c => c.Longitude)
            .HasPrecision(10, 6);
    }
}
