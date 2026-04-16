using Core.Domain.Entities.StudyGroup;
using Core.Domain.Entities.TeacherStaff;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Infrastructure.Configurations;

public sealed class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.HasKey(g => g.Id);
        
        builder.Property(g => g.Name).IsRequired().HasMaxLength(100);
        builder.Property(g => g.Year).IsRequired().HasMaxLength(20);
        builder.Property(g => g.EducationLevel).IsRequired().HasConversion<string>();

        builder.HasMany(g => g.Students)
            .WithOne(s => s.Group)
            .HasForeignKey(s => s.GroupId)
            .OnDelete(DeleteBehavior.Restrict);

        // 1-to-1: Group (Principal) <-> DiplomaExaminationCommission (Dependent)
        builder.HasOne(g => g.DiplomaExaminationCommission)
            .WithOne(dec => dec.Group)
            .HasForeignKey<DiplomaExaminationCommission>(dec => dec.GroupId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
