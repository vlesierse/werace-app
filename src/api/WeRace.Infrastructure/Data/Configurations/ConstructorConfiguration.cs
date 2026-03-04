using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeRace.Domain.Entities;

namespace WeRace.Infrastructure.Data.Configurations;

public class ConstructorConfiguration : IEntityTypeConfiguration<Constructor>
{
    public void Configure(EntityTypeBuilder<Constructor> builder)
    {
        builder.ToTable("constructors");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.ConstructorRef)
            .HasMaxLength(255)
            .IsRequired();

        builder.HasIndex(c => c.ConstructorRef)
            .IsUnique();

        builder.Property(c => c.Name)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(c => c.Nationality)
            .HasMaxLength(255);
    }
}
