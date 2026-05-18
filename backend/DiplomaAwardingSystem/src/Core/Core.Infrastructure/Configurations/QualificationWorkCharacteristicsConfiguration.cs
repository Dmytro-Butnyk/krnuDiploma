using Core.Domain.Entities.StudentDiplomaData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Infrastructure.Configurations;

public sealed class QualificationWorkCharacteristicsConfiguration : IEntityTypeConfiguration<QualificationWorkCharacteristics>
{
    public void Configure(EntityTypeBuilder<QualificationWorkCharacteristics> builder)
    {
        builder.HasKey(qwc => qwc.Id);

        builder.Property(qwc => qwc.IsResearchBased).IsRequired();
        builder.Property(qwc => qwc.HasRealProjects).IsRequired();
        builder.Property(qwc => qwc.IsEcoFriendly).IsRequired();
        builder.Property(qwc => qwc.IsEnterpriseOrdered).IsRequired();
        builder.Property(qwc => qwc.IsComplexInteruniversity).IsRequired();
        builder.Property(qwc => qwc.IsComplexInterdepartmental).IsRequired();
        builder.Property(qwc => qwc.IsComplexDepartmental).IsRequired();
        builder.Property(qwc => qwc.IsComplexProjectParticipant).IsRequired();
        builder.Property(qwc => qwc.IsRecommendedForMaster).IsRequired();
        builder.Property(qwc => qwc.IsRecommendedForImplementation).IsRequired();
        builder.Property(qwc => qwc.IsDefendedAtEnterprise).IsRequired();
    }
}
