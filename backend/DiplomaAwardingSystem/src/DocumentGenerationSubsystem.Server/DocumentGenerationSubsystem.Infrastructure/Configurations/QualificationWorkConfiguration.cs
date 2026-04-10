using Core.Domain.Entities.ArchiveGroup;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocumentGenerationSubsystem.Infrastructure.Configurations;

public sealed class QualificationWorkConfiguration : IEntityTypeConfiguration<QualificationWork>
{
    public void Configure(EntityTypeBuilder<QualificationWork> builder)
    {
        builder.HasKey(qw => qw.Id);
        
        builder.Property(qw => qw.Topic).IsRequired().HasMaxLength(500);
        builder.Property(qw => qw.EctsGrade).IsRequired().HasConversion<string>();
        builder.Property(qw => qw.NationalGrade).IsRequired().HasConversion<string>();

        // 1-to-1: QualificationWork (Principal) <-> Defence (Dependent)
        builder.HasOne(qw => qw.Defence)
            .WithOne(d => d.QualificationWork)
            .HasForeignKey<Defence>(d => d.QualificationWorkId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
