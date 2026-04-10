using Core.Domain.Entities.TeacherStaff;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocumentGenerationSubsystem.Infrastructure.Configurations;

public sealed class DecMemberConfiguration : IEntityTypeConfiguration<DecMember>
{
    public void Configure(EntityTypeBuilder<DecMember> builder)
    {
        builder.HasKey(dm => dm.Id);
        
        builder.Property(dm => dm.Role).IsRequired().HasConversion<string>();

        builder.HasMany(dm => dm.DecToMembers)
            .WithOne(dtm => dtm.DecMember)
            .HasForeignKey(dtm => dtm.DecMemberId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
