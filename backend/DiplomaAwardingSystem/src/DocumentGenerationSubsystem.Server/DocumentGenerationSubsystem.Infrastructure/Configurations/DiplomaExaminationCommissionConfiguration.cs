using Core.Domain.Entities.ArchiveGroup;
using Core.Domain.Entities.TeacherStaff;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocumentGenerationSubsystem.Infrastructure.Configurations;

public sealed class DiplomaExaminationCommissionConfiguration : IEntityTypeConfiguration<DiplomaExaminationCommission>
{
    public void Configure(EntityTypeBuilder<DiplomaExaminationCommission> builder)
    {
        builder.HasKey(dec => dec.Id);

        // 1-to-1: DEC (Principal) <-> Archive (Dependent)
        builder.HasOne(dec => dec.Archive)
            .WithOne(a => a.DiplomaExaminationCommission)
            .HasForeignKey<Archive>(a => a.DiplomaExaminationCommissionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Many-to-Many via DecToMember
        builder.HasMany(dec => dec.DecToMembers)
            .WithOne(dtm => dtm.DiplomaExaminationCommission)
            .HasForeignKey(dtm => dtm.DiplomaExaminationCommissionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(dec => dec.Defences)
            .WithOne(def => def.DiplomaExaminationCommission)
            .HasForeignKey(def => def.DiplomaExaminationCommissionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
