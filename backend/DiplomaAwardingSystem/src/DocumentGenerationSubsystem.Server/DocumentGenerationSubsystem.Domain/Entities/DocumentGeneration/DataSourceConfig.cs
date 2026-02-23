using System.Text.Json.Serialization;

namespace DocumentGenerationSubsystem.Domain.Entities.DocumentGeneration;

public record DataSourceConfig(
    [property: JsonPropertyName("Key")] string Key,
    [property: JsonPropertyName("Entity")] string Entity,
    [property: JsonPropertyName("Filter")] string Filter,
    [property: JsonPropertyName("FilterArgs")] IReadOnlyCollection<string> FilterArgs,
    [property: JsonPropertyName("Includes")] IReadOnlyCollection<string> Includes
);
