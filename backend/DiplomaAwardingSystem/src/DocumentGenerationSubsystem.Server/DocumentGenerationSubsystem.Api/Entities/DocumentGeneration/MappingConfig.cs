using System.Text.Json.Serialization;

namespace DocumentGenerationSubsystem.Api.Entities.DocumentGeneration;

public record MappingConfig(
    [property: JsonPropertyName("Scalars")] Dictionary<string, string>? Scalars,
    [property: JsonPropertyName("Tables")] Dictionary<string, TableMappingConfig>? Tables
);
