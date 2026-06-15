using Core.Domain.Entities.TeacherStaff;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Infrastructure.Configurations;

public sealed class TeacherPositionConfiguration : IEntityTypeConfiguration<TeacherPosition>
{
    public void Configure(EntityTypeBuilder<TeacherPosition> builder)
    {
        builder.HasKey(tp => tp.Id);

        builder.Property(tp => tp.FullName).IsRequired().HasMaxLength(256);
        builder.Property(tp => tp.ShortName).IsRequired().HasMaxLength(256);
        builder.Property(tp => tp.GenitiveFullName).IsRequired().HasMaxLength(256);
        builder.Property(tp => tp.GenitiveShortName).IsRequired().HasMaxLength(256);
        builder.Property(tp => tp.IsActive).IsRequired();

        builder.HasMany(tp => tp.Teachers)
            .WithOne(t => t.TeacherPosition)
            .HasForeignKey(t => t.TeacherPositionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
