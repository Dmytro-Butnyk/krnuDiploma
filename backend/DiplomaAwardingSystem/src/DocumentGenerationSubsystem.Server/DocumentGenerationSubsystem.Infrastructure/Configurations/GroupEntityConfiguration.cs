using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocumentGenerationSubsystem.Infrastructure.Configurations;

public sealed class GroupEntityConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.ToTable("groups");

        ConfigureBasicProperties(builder);
        ConfigureTeacherRelation(builder);
    }

    private static void ConfigureBasicProperties(EntityTypeBuilder<Group> builder)
    {
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(g => g.Specialty)
            .IsRequired()
            .HasMaxLength(200);
    }

    private static void ConfigureTeacherRelation(EntityTypeBuilder<Group> builder)
    {
        builder.HasOne(g => g.Teacher)
            .WithOne()
            .HasForeignKey<Group>(g => g.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
