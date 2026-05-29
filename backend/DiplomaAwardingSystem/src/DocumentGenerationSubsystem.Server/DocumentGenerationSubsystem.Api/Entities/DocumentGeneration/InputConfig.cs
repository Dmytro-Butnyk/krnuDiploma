using System.Text.Json.Serialization;

namespace DocumentGenerationSubsystem.Api.Entities.DocumentGeneration;

public sealed record InputConfig(
    [property: JsonPropertyName("Kind")] string Kind,
    [property: JsonPropertyName("ValueType")] string ValueType,
    [property: JsonPropertyName("Label")] string? Label,
    [property: JsonPropertyName("Required")] bool Required,
    [property: JsonPropertyName("Entity")] string? Entity,
    [property: JsonPropertyName("DependsOn")] IReadOnlyCollection<string>? DependsOn,
    [property: JsonPropertyName("Filters")] IReadOnlyCollection<InputFilterConfig>? Filters,
    [property: JsonPropertyName("Display")] IReadOnlyCollection<string>? Display,
    [property: JsonPropertyName("Description")] IReadOnlyCollection<string>? Description,
    [property: JsonPropertyName("Search")] IReadOnlyCollection<string>? Search,
    [property: JsonPropertyName("OrderBy")] IReadOnlyCollection<string>? OrderBy,
    [property: JsonPropertyName("MaxLength")] int? MaxLength
);
