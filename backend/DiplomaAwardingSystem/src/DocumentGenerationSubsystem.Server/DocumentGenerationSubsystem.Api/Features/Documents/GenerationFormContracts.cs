using DocumentGenerationSubsystem.Api.Entities.DocumentGeneration;

#pragma warning disable SA1402, SA1649, SA1118

namespace DocumentGenerationSubsystem.Api.Features.Documents;

public sealed record GenerationFormDto(
    int ConfigurationVersion,
    IReadOnlyCollection<GenerationInputDto> Inputs);

public sealed record GenerationInputDto(
    string Key,
    string Kind,
    string ValueType,
    string Label,
    bool Required,
    string? Entity,
    string? ValuePath,
    IReadOnlyCollection<string> DependsOn,
    IReadOnlyCollection<InputFilterConfig> Filters,
    IReadOnlyCollection<string> Display,
    IReadOnlyCollection<string> Description,
    IReadOnlyCollection<string> Search,
    IReadOnlyCollection<string> OrderBy,
    int? MaxLength,
    string? OptionsEndpoint);

internal static class GenerationFormMapper
{
    public static GenerationFormDto Map(int templateId, TemplateConfiguration configuration)
    {
        var inputs = configuration.Inputs?
            .Select(input => MapInput(templateId, input.Key, input.Value))
            .ToArray()
            ?? [];

        return new GenerationFormDto(configuration.ConfigurationVersion, inputs);
    }

    private static GenerationInputDto MapInput(int templateId, string key, InputConfig input)
    {
        var hasOptionsEndpoint =
            string.Equals(input.Kind, InputKinds.EntitySelect, StringComparison.OrdinalIgnoreCase)
            || string.Equals(input.Kind, InputKinds.ValueSelect, StringComparison.OrdinalIgnoreCase);

        return new GenerationInputDto(
            key,
            input.Kind,
            input.ValueType,
            string.IsNullOrWhiteSpace(input.Label) ? key : input.Label,
            input.Required,
            input.Entity,
            input.ValuePath,
            input.DependsOn?.ToArray() ?? [],
            input.Filters?.ToArray() ?? [],
            input.Display?.ToArray() ?? [],
            input.Description?.ToArray() ?? [],
            input.Search?.ToArray() ?? [],
            input.OrderBy?.ToArray() ?? [],
            input.MaxLength,
            hasOptionsEndpoint
                ? $"/api/documents/templates/{templateId}/generation-inputs/{key}/options"
                : null);
    }
}
