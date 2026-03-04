using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeRace.Domain.Entities;

namespace WeRace.Infrastructure.Data.Configurations;

public class SeasonConfiguration : IEntityTypeConfiguration<Season>
{
    public void Configure(EntityTypeBuilder<Season> builder)
    {
        builder.ToTable("seasons");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Year)
            .IsRequired();

        builder.HasIndex(s => s.Year)
            .IsUnique();
    }
}
