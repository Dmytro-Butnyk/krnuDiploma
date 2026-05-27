using Core.Domain.Entities.StudyGroup;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Infrastructure.Configurations;

public sealed class SpecialtyConfiguration : IEntityTypeConfiguration<Specialty>
{
    public void Configure(EntityTypeBuilder<Specialty> builder)
    {
        builder.HasKey(s => s.Id);
        
        builder.Property(s => s.Code).IsRequired().HasMaxLength(50);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(256);
        builder.Property(s => s.IsActive).IsRequired();
        builder.HasIndex(s => s.Code).IsUnique();

        builder.HasMany(s => s.Groups)
            .WithOne(g => g.Specialty)
            .HasForeignKey(g => g.SpecialtyId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasMany(d => d.Teachers)
            .WithOne(t => t.Specialty)
            .HasForeignKey(t => t.SpecialtyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Secretaries)
            .WithOne(sec => sec.Specialty)
            .HasForeignKey(sec => sec.SpecialtyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.DiplomaExaminationCommissions)
            .WithOne(dec => dec.Specialty)
            .HasForeignKey(dec => dec.SpecialtyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
