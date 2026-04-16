using Core.Domain.Entities.TeacherStaff;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Infrastructure.Configurations;

public sealed class DecToMemberConfiguration : IEntityTypeConfiguration<DecToMember>
{
    public void Configure(EntityTypeBuilder<DecToMember> builder)
    {
        builder.HasKey(dtm => dtm.Id);

        // Уникальный составной индекс для предотвращения дублей в M:M
        builder.HasIndex(dtm => new { dtm.DecMemberId, dtm.DiplomaExaminationCommissionId }).IsUnique();
    }
}
