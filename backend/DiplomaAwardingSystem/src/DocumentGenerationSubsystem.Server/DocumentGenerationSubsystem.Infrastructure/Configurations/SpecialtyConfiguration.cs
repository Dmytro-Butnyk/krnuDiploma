using Core.Domain.Entities.StudyGroup;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocumentGenerationSubsystem.Infrastructure.Configurations;

public sealed class SpecialtyConfiguration : IEntityTypeConfiguration<Specialty>
{
    public void Configure(EntityTypeBuilder<Specialty> builder)
    {
        builder.HasKey(s => s.Id);
        
        builder.Property(s => s.Code).IsRequired().HasMaxLength(50);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(256);
        builder.HasIndex(s => s.Code).IsUnique();

        builder.HasMany(s => s.Groups)
            .WithOne(g => g.Specialty)
            .HasForeignKey(g => g.SpecialtyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
