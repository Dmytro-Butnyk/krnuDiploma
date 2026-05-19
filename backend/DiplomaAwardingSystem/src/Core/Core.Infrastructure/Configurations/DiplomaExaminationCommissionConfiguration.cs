using Core.Domain.Entities.ArchiveGroup;
using Core.Domain.Entities.TeacherStaff;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Infrastructure.Configurations;

public sealed class DiplomaExaminationCommissionConfiguration : IEntityTypeConfiguration<DiplomaExaminationCommission>
{
    public void Configure(EntityTypeBuilder<DiplomaExaminationCommission> builder)
    {
        builder.HasKey(dec => dec.Id);

        builder.Property(dec => dec.EducationLevel).IsRequired().HasConversion<string>();
        builder.Property(dec => dec.HeadPersonaName).HasMaxLength(256);
        builder.Property(dec => dec.HeadPersonaPosition).HasMaxLength(256);

        builder.HasOne(dec => dec.Archive)
            .WithOne(a => a.DiplomaExaminationCommission)
            .HasForeignKey<Archive>(a => a.DiplomaExaminationCommissionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(dec => dec.HeadTeacher)
            .WithMany()
            .HasForeignKey(dec => dec.HeadTeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(dec => dec.FirstMemberTeacher)
            .WithMany()
            .HasForeignKey(dec => dec.FirstMemberTeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(dec => dec.SecondMemberTeacher)
            .WithMany()
            .HasForeignKey(dec => dec.SecondMemberTeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(dec => dec.ThirdMemberTeacher)
            .WithMany()
            .HasForeignKey(dec => dec.ThirdMemberTeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(dec => dec.Secretary)
            .WithMany()
            .HasForeignKey(dec => dec.SecretaryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
