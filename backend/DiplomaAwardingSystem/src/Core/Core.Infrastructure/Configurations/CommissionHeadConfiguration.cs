using Core.Domain.Entities.TeacherStaff;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Infrastructure.Configurations;

public sealed class CommissionHeadConfiguration : IEntityTypeConfiguration<CommissionHead>
{
    public void Configure(EntityTypeBuilder<CommissionHead> builder)
    {
        builder.HasKey(ch => ch.Id);

        builder.Property(ch => ch.FullName).IsRequired().HasMaxLength(256);
        builder.Property(ch => ch.Position).IsRequired().HasMaxLength(256);
        builder.Property(ch => ch.Company).IsRequired().HasMaxLength(256);
        builder.Property(ch => ch.Specialty).IsRequired().HasMaxLength(256);
        builder.Property(ch => ch.IsDeleted).IsRequired();

        builder.HasIndex(ch => new
            {
                ch.FullName,
                ch.Position,
                ch.Company,
                ch.Specialty
            })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");
    }
}
