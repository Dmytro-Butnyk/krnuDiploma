using System.Text.Json.Serialization;

namespace DocumentGenerationSubsystem.Api.Entities.DocumentGeneration;

public record DataSourceConfig(
    [property: JsonPropertyName("Key")] string Key,
    [property: JsonPropertyName("Entity")] string Entity,
    [property: JsonPropertyName("Result")] string? Result,
    [property: JsonPropertyName("Filter")] string? Filter,
    [property: JsonPropertyName("FilterArgs")] IReadOnlyCollection<string>? FilterArgs,
    [property: JsonPropertyName("Includes")] IReadOnlyCollection<string>? Includes,
    [property: JsonPropertyName("OrderBy")] IReadOnlyCollection<string>? OrderBy
);
