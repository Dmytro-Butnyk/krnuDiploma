using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocumentGenerationSubsystem.Infrastructure.Configurations;

public sealed class QualificationWorkEntityConfiguration : IEntityTypeConfiguration<QualificationWork>
{
    public void Configure(EntityTypeBuilder<QualificationWork> builder)
    {
        builder.ToTable("qualification_works");

        ConfigureBasicProperties(builder);
        ConfigureStudentRelation(builder);
        ConfigureTeacherRelation(builder);
    }

    private static void ConfigureBasicProperties(EntityTypeBuilder<QualificationWork> builder)
    {
        builder.HasKey(qw => qw.Id);

        builder.Property(qw => qw.Name)
            .IsRequired()
            .HasMaxLength(500);
    }

    private static void ConfigureStudentRelation(EntityTypeBuilder<QualificationWork> builder)
    {
        builder.HasOne(qw => qw.Student)
            .WithOne()
            .HasForeignKey<QualificationWork>(qw => qw.StudentId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureTeacherRelation(EntityTypeBuilder<QualificationWork> builder)
    {
        builder.HasOne(qw => qw.Teacher)
            .WithMany()
            .HasForeignKey(qw => qw.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
