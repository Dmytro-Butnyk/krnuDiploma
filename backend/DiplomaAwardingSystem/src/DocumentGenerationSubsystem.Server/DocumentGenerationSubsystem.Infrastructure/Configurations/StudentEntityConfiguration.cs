using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocumentGenerationSubsystem.Infrastructure.Configurations;

public sealed class StudentEntityConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("students");
        
        ConfigureBasicProperties(builder);
        ConfigureGroupRelation(builder);
        ConfigureQualificationWorkRelation(builder);
    }

    private static void ConfigureBasicProperties(EntityTypeBuilder<Student> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.FullName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(s => s.Gender)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(10);
    }

    private static void ConfigureGroupRelation(EntityTypeBuilder<Student> builder)
    {
        builder.HasOne(s => s.Group)
            .WithMany(g => g.Students)
            .HasForeignKey(s => s.GroupId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureQualificationWorkRelation(EntityTypeBuilder<Student> builder)
    {
        builder.HasOne(s => s.QualificationWork)
            .WithOne(qw => qw.Student)
            .HasForeignKey<QualificationWork>(qw => qw.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
