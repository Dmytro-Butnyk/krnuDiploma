using Core.Domain.Entities.ArchiveGroup;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocumentGenerationSubsystem.Infrastructure.Configurations;

public sealed class ArchiveConfiguration : IEntityTypeConfiguration<Archive>
{
    public void Configure(EntityTypeBuilder<Archive> builder)
    {
        builder.HasKey(a => a.Id);
        
        builder.Property(a => a.ProtocolRange).HasMaxLength(100);
        builder.Property(a => a.CaseNumber).HasMaxLength(100);
    }
}
