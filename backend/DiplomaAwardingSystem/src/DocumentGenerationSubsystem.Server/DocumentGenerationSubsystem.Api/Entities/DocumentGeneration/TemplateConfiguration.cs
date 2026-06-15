using System.Text.Json.Serialization;

namespace DocumentGenerationSubsystem.Api.Entities.DocumentGeneration;

public sealed record TemplateConfiguration(
    [property: JsonPropertyName("ConfigurationVersion")] int ConfigurationVersion,
    [property: JsonPropertyName("ScenarioCode")] string? ScenarioCode,
    [property: JsonPropertyName("Inputs")] IReadOnlyDictionary<string, InputConfig>? Inputs,
    [property: JsonPropertyName("DataSources")] IReadOnlyCollection<DataSourceConfig>? DataSources,
    [property: JsonPropertyName("Mapping")] MappingConfig? Mapping
);
