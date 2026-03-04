using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeRace.Domain.Entities;

namespace WeRace.Infrastructure.Data.Configurations;

public class ConstructorResultConfiguration : IEntityTypeConfiguration<ConstructorResult>
{
    public void Configure(EntityTypeBuilder<ConstructorResult> builder)
    {
        builder.ToTable("constructor_results");

        builder.HasKey(cr => cr.Id);

        builder.Property(cr => cr.Points)
            .HasPrecision(5, 2);

        builder.Property(cr => cr.Status)
            .HasMaxLength(255);

        builder.HasIndex(cr => cr.RaceId);
        builder.HasIndex(cr => cr.ConstructorId);

        builder.HasOne(cr => cr.Race)
            .WithMany(r => r.ConstructorResults)
            .HasForeignKey(cr => cr.RaceId)
            .IsRequired();

        builder.HasOne(cr => cr.Constructor)
            .WithMany(c => c.ConstructorResults)
            .HasForeignKey(cr => cr.ConstructorId)
            .IsRequired();
    }
}
