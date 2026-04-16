using Core.Domain.Entities.ArchiveGroup;
using Core.Domain.Entities.StudyGroup;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Infrastructure.Configurations;

public sealed class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.HasKey(s => s.Id);
        
        builder.Property(s => s.FullName).IsRequired().HasMaxLength(256);

        // 1-to-1: Student (Principal) <-> QualificationWork (Dependent)
        builder.HasOne(s => s.QualificationWork)
            .WithOne(qw => qw.Student)
            .HasForeignKey<QualificationWork>(qw => qw.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
