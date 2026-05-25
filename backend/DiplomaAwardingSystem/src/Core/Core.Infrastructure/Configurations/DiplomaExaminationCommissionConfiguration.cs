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

        builder.Property(dec => dec.OrderNumber).IsRequired().HasMaxLength(64);
        builder.Property(dec => dec.EducationLevel).IsRequired().HasConversion<string>();
        builder.Property(dec => dec.DefenseYear).IsRequired().HasMaxLength(20);

        builder.HasIndex(dec => new
        {
            dec.DefenseYear,
            dec.SpecialtyId,
            dec.EducationLevel
        }).IsUnique();

        builder.HasIndex(dec => new
        {
            dec.DefenseYear,
            dec.SpecialtyId,
            dec.OrderNumber
        }).IsUnique();

        builder.HasOne(dec => dec.Archive)
            .WithOne(a => a.DiplomaExaminationCommission)
            .HasForeignKey<Archive>(a => a.DiplomaExaminationCommissionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(dec => dec.Specialty)
            .WithMany(s => s.DiplomaExaminationCommissions)
            .HasForeignKey(dec => dec.SpecialtyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(dec => dec.CommissionHead)
            .WithMany(head => head.DiplomaExaminationCommissions)
            .HasForeignKey(dec => dec.CommissionHeadId)
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
