using Core.Domain.Entities.TeacherStaff;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Infrastructure.Configurations;

public sealed class AcademicDegreeConfiguration : IEntityTypeConfiguration<AcademicDegree>
{
    public void Configure(EntityTypeBuilder<AcademicDegree> builder)
    {
        builder.HasKey(a => a.Id);
        
        builder.Property(a => a.FullName).IsRequired().HasMaxLength(256);
        builder.Property(a => a.ShortName).IsRequired().HasMaxLength(50);

        builder.HasMany(a => a.Teachers)
            .WithOne(t => t.AcademicDegree)
            .HasForeignKey(t => t.AcademicDegreeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
