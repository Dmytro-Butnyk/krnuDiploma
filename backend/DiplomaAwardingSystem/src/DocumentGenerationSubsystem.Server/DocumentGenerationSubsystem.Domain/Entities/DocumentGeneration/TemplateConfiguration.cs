using System.Text.Json.Serialization;

namespace DocumentGenerationSubsystem.Domain.Entities.DocumentGeneration;

public sealed record TemplateConfiguration(
    [property: JsonPropertyName("DataSources")] IReadOnlyCollection<DataSourceConfig> DataSources,
    [property: JsonPropertyName("Mapping")] MappingConfig Mapping
);
