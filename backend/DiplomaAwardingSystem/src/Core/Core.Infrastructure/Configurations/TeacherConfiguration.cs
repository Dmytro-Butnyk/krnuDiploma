using Core.Domain.Entities.TeacherStaff;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Infrastructure.Configurations;

public sealed class TeacherConfiguration : IEntityTypeConfiguration<Teacher>
{
    public void Configure(EntityTypeBuilder<Teacher> builder)
    {
        builder.HasKey(t => t.Id);
        
        builder.Property(t => t.FullName).IsRequired().HasMaxLength(256);
        builder.Property(t => t.ShortName).IsRequired().HasMaxLength(100);
        builder.Property(t => t.Email).IsRequired().HasMaxLength(150);
        builder.Property(t => t.PhoneNumber).HasMaxLength(50);
        builder.Property(t => t.Position).HasMaxLength(150);
        
        builder.HasIndex(t => t.Email).IsUnique();

        builder.HasMany(t => t.QualificationWorks)
            .WithOne(qw => qw.Teacher)
            .HasForeignKey(qw => qw.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.ReviewedQualificationWorks)
            .WithOne(qw => qw.Reviewer)
            .HasForeignKey(qw => qw.ReviewerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.DecMembers)
            .WithOne(dm => dm.Teacher)
            .HasForeignKey(dm => dm.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
