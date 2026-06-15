namespace DocumentGenerationSubsystem.Api.Entities.DocumentGeneration;

public sealed record ConstructorScenarioDefinition(
    string Id,
    string Title,
    string Description,
    IReadOnlyDictionary<string, InputConfig> Inputs,
    IReadOnlyCollection<DataSourceConfig> DataSources,
    IReadOnlyCollection<ScenarioTableSourceDefinition> RecommendedTableSources,
    IReadOnlyCollection<ScenarioScalarMappingDefinition> RequiredScalarMappings,
    IReadOnlyCollection<ScenarioTableRequirementDefinition> RequiredTableSources,
    IReadOnlyCollection<string> HelperKeys);
