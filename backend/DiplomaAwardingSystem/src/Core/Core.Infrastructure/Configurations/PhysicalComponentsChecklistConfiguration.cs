using Core.Domain.Entities.StudentDiplomaData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Infrastructure.Configurations;

public sealed class PhysicalComponentsChecklistConfiguration : IEntityTypeConfiguration<PhysicalComponentsChecklist>
{
    public void Configure(EntityTypeBuilder<PhysicalComponentsChecklist> builder)
    {
        builder.HasKey(pcc => pcc.Id);

        builder.Property(pcc => pcc.HasStudentCard).IsRequired();
        builder.Property(pcc => pcc.HasGradeBook).IsRequired();
        builder.Property(pcc => pcc.HasCircular).IsRequired();
        builder.Property(pcc => pcc.HasSignedReview).IsRequired();
        builder.Property(pcc => pcc.HasCopyOfBankReceipt).IsRequired();
        builder.Property(pcc => pcc.HasExplanatoryNote).IsRequired();
    }
}
