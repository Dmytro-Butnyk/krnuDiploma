using Core.Domain.Entities.ArchiveGroup;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocumentGenerationSubsystem.Infrastructure.Configurations;

public sealed class DefenceConfiguration : IEntityTypeConfiguration<Defence>
{
    public void Configure(EntityTypeBuilder<Defence> builder)
    {
        builder.HasKey(d => d.Id);
        
        builder.Property(d => d.ProtocolNumber).HasMaxLength(100);
    }
}
