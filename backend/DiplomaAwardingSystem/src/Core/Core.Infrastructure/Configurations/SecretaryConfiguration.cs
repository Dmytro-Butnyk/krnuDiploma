using Core.Domain.Entities.StudyGroup;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Infrastructure.Configurations;

public sealed class SecretaryConfiguration : IEntityTypeConfiguration<Secretary>
{
    public void Configure(EntityTypeBuilder<Secretary> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Email).IsRequired().HasMaxLength(320);
        builder.Property(s => s.FullName).IsRequired().HasMaxLength(256);
        builder.Property(s => s.IsActive).IsRequired();

        builder.HasIndex(s => s.Email).IsUnique();
    }
}
