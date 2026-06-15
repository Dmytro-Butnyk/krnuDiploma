using DocumentGenerationSubsystem.Api.Entities.DocumentGeneration;

namespace DocumentGenerationSubsystem.Api.Infrastructure.Scenarios;

public sealed class DocumentScenarioContext
{
    public required string ScenarioCode { get; init; }
    public required TemplateConfiguration Configuration { get; init; }
    public required IReadOnlyDictionary<string, string> Parameters { get; init; }
    public required IReadOnlyDictionary<string, object> DataContext { get; init; }
}
