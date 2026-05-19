using Core.Domain.Entities.ArchiveGroup;
using Core.Domain.Entities.StudentDiplomaData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Infrastructure.Configurations;

public sealed class QualificationWorkConfiguration : IEntityTypeConfiguration<QualificationWork>
{
    public void Configure(EntityTypeBuilder<QualificationWork> builder)
    {
        builder.HasKey(qw => qw.Id);
        
        builder.Property(qw => qw.Topic).IsRequired().HasMaxLength(500);
        builder.Property(qw => qw.PracticeBase).HasMaxLength(256);
        builder.Property(qw => qw.EctsGrade).IsRequired().HasConversion<string>();
        builder.Property(qw => qw.NationalGrade).IsRequired().HasConversion<string>();

        // 1-to-1: QualificationWork (Principal) <-> Defence (Dependent)
        builder.HasOne(qw => qw.Defence)
            .WithOne(d => d.QualificationWork)
            .HasForeignKey<Defence>(d => d.QualificationWorkId)
            .OnDelete(DeleteBehavior.Cascade);

        // 1-to-1: QualificationWork (Principal) <-> QualificationWorkCharacteristics (Dependent)
        builder.HasOne(qw => qw.QualificationWorkCharacteristics)
            .WithOne(qwc => qwc.QualificationWork)
            .HasForeignKey<QualificationWorkCharacteristics>(qwc => qwc.QualificationWorkId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
