using System.Text.Json.Serialization;

namespace DocumentGenerationSubsystem.Domain.Entities.DocumentGeneration;

public record TableMappingConfig(
    [property: JsonPropertyName("SourceArray")] string SourceArray,
    [property: JsonPropertyName("RowMapping")] Dictionary<string, string> RowMapping
);
