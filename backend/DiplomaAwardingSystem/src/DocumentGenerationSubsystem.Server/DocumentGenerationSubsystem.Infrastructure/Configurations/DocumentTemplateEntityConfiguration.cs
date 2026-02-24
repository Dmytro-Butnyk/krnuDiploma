using DocumentGenerationSubsystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocumentGenerationSubsystem.Infrastructure.Configurations;

public sealed class DocumentTemplateEntityConfiguration : IEntityTypeConfiguration<DocumentTemplate>
{
    public void Configure(EntityTypeBuilder<DocumentTemplate> builder)
    {
        builder.ToTable("document_templates");

        builder.HasKey(dt => dt.Id);

        builder.Property(dt => dt.Name)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(dt => dt.WordTemplate)
            .IsRequired();

        builder.Property(dt => dt.ConfigurationJson)
            .IsRequired()
            .HasColumnType("jsonb");
    }
}
