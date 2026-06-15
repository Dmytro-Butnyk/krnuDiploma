using DocumentGenerationSubsystem.Api.Entities.DocumentGeneration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocumentGenerationSubsystem.Api.Infrastructure;

public sealed class DocumentConstructorScenarioEntityConfiguration : IEntityTypeConfiguration<DocumentConstructorScenario>
{
    public void Configure(EntityTypeBuilder<DocumentConstructorScenario> builder)
    {
        builder.ToTable("DocumentConstructorScenarios");

        builder.HasKey(scenario => scenario.Id);

        builder.Property(scenario => scenario.Code)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(scenario => scenario.Title)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(scenario => scenario.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(scenario => scenario.ScenarioJson)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(scenario => scenario.IsActive)
            .IsRequired();

        builder.HasIndex(scenario => scenario.Code)
            .IsUnique();
    }
}
