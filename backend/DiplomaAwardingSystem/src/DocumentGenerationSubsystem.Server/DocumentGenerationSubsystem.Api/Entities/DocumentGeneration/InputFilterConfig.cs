using System.Text.Json.Serialization;

namespace DocumentGenerationSubsystem.Api.Entities.DocumentGeneration;

public sealed record InputFilterConfig(
    [property: JsonPropertyName("Property")] string Property,
    [property: JsonPropertyName("Operator")] string Operator,
    [property: JsonPropertyName("Input")] string Input
);
