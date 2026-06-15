using Core.Domain.Entities.ArchiveGroup;
using Core.Domain.Entities.StudyGroup;
using Core.Domain.Entities.StudentDiplomaData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Infrastructure.Configurations;

public sealed class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.HasKey(s => s.Id);
        
        builder.Property(s => s.FullName).IsRequired().HasMaxLength(256);

        builder.OwnsOne(
            s => s.NameForms,
            nameForms =>
            {
                nameForms.Property(n => n.Nominative)
                    .HasColumnName("NameNominative")
                    .IsRequired()
                    .HasMaxLength(256);
                nameForms.Property(n => n.Genitive)
                    .HasColumnName("NameGenitive")
                    .IsRequired()
                    .HasMaxLength(256);
                nameForms.Property(n => n.Dative)
                    .HasColumnName("NameDative")
                    .IsRequired()
                    .HasMaxLength(256);
                nameForms.Property(n => n.Signature)
                    .HasColumnName("NameSignature")
                    .IsRequired()
                    .HasMaxLength(256);
            });

        builder.HasIndex(s => new { s.GroupId, s.FullName });

        // 1-to-1: Student (Principal) <-> QualificationWork (Dependent)
        builder.HasOne(s => s.QualificationWork)
            .WithOne(qw => qw.Student)
            .HasForeignKey<QualificationWork>(qw => qw.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        // 1-to-1: Student (Principal) <-> ElectronicComponentsChecklist (Dependent)
        builder.HasOne(s => s.ElectronicComponentsChecklist)
            .WithOne(ecc => ecc.Student)
            .HasForeignKey<ElectronicComponentsChecklist>(ecc => ecc.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        // 1-to-1: Student (Principal) <-> PhysicalComponentsChecklist (Dependent)
        builder.HasOne(s => s.PhysicalComponentsChecklist)
            .WithOne(pcc => pcc.Student)
            .HasForeignKey<PhysicalComponentsChecklist>(pcc => pcc.StudentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
