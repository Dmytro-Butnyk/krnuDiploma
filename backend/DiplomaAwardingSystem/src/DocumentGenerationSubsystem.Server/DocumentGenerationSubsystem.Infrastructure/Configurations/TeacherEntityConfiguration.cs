using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocumentGenerationSubsystem.Infrastructure.Configurations;

public sealed class TeacherEntityConfiguration : IEntityTypeConfiguration<Teacher>
{
    public void Configure(EntityTypeBuilder<Teacher> builder)
    {
        builder.ToTable("teachers");

        ConfigureBasicProperties(builder);
        ConfigureGroupRelation(builder);
        ConfigureQualificationWorksRelation(builder);
    }

    private static void ConfigureBasicProperties(EntityTypeBuilder<Teacher> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.FullName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(t => t.Position)
            .IsRequired()
            .HasMaxLength(500);
    }

    private static void ConfigureGroupRelation(EntityTypeBuilder<Teacher> builder)
    {
        builder.HasOne(t => t.CuratedGroup)
            .WithOne(g => g.Teacher)
            .HasForeignKey<Group>(g => g.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureQualificationWorksRelation(EntityTypeBuilder<Teacher> builder)
    {
        builder.HasMany(t => t.SupervisedWorks)
            .WithOne(qw => qw.Teacher)
            .HasForeignKey(qw => qw.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
