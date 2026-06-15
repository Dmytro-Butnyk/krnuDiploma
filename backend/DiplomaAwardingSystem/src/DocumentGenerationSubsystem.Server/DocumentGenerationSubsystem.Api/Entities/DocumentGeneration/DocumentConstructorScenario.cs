using Core.Domain.Entities;

namespace DocumentGenerationSubsystem.Api.Entities.DocumentGeneration;

public sealed class DocumentConstructorScenario : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ScenarioJson { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    private DocumentConstructorScenario()
    {
    }

    public DocumentConstructorScenario(string code, string title, string description, string scenarioJson)
    {
        Code = code;
        Title = title;
        Description = description;
        ScenarioJson = scenarioJson;
        IsActive = true;
    }
}
