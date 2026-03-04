using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeRace.Domain.Entities;

namespace WeRace.Infrastructure.Data.Configurations;

public class DriverConfiguration : IEntityTypeConfiguration<Driver>
{
    public void Configure(EntityTypeBuilder<Driver> builder)
    {
        builder.ToTable("drivers");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.DriverRef)
            .HasMaxLength(255)
            .IsRequired();

        builder.HasIndex(d => d.DriverRef)
            .IsUnique();

        builder.Property(d => d.Code)
            .HasMaxLength(3);

        builder.HasIndex(d => d.Code);

        builder.Property(d => d.Forename)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(d => d.Surname)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(d => d.Nationality)
            .HasMaxLength(255);
    }
}
