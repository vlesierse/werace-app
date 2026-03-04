using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeRace.Domain.Entities;

namespace WeRace.Infrastructure.Data.Configurations;

public class StatusConfiguration : IEntityTypeConfiguration<Status>
{
    public void Configure(EntityTypeBuilder<Status> builder)
    {
        builder.ToTable("status");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.StatusText)
            .HasColumnName("status")
            .HasMaxLength(255)
            .IsRequired();
    }
}
