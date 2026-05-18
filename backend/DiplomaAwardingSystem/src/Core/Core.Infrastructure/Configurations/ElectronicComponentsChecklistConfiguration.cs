using Core.Domain.Entities.StudentDiplomaData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Infrastructure.Configurations;

public sealed class ElectronicComponentsChecklistConfiguration : IEntityTypeConfiguration<ElectronicComponentsChecklist>
{
    public void Configure(EntityTypeBuilder<ElectronicComponentsChecklist> builder)
    {
        builder.HasKey(ecc => ecc.Id);

        builder.Property(ecc => ecc.HasRegulatoryControl).IsRequired();
        builder.Property(ecc => ecc.HasExplanatoryNoteDoc).IsRequired();
        builder.Property(ecc => ecc.HasExplanatoryNotePdf).IsRequired();
        builder.Property(ecc => ecc.HasPlagiarismReportPdf).IsRequired();
        builder.Property(ecc => ecc.HasReviewDoc).IsRequired();
        builder.Property(ecc => ecc.HasPresentationPpt).IsRequired();
    }
}
