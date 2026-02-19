using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocumentGenerationSubsystem.Infrastructure.Configurations;

public sealed class RectorEntityConfiguration : IEntityTypeConfiguration<Rector>
{
    public void Configure(EntityTypeBuilder<Rector> builder)
    {
        builder.ToTable("rectors");

        ConfigureBasicProperties(builder);
    }

    private static void ConfigureBasicProperties(EntityTypeBuilder<Rector> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.FullName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(r => r.IsActive)
            .IsRequired();
    }
}
