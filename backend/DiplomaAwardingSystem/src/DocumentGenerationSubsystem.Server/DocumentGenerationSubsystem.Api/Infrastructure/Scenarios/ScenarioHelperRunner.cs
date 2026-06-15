using System.Text.Json;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using DocumentGenerationSubsystem.Api.Entities.DocumentGeneration;
using Microsoft.EntityFrameworkCore;

namespace DocumentGenerationSubsystem.Api.Infrastructure.Scenarios;

public sealed class ScenarioHelperRunner(
    DbDocGenContext dbContext,
    IEnumerable<IDocumentScenarioHelper> helpers)
    : IScopedService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly Dictionary<string, IDocumentScenarioHelper> helpersByKey = helpers
        .ToDictionary(helper => helper.Key, StringComparer.OrdinalIgnoreCase);

    public async Task<Result<Dictionary<string, object>>> BuildComputedContextAsync(
        TemplateConfiguration configuration,
        IReadOnlyDictionary<string, string> parameters,
        IReadOnlyDictionary<string, object> dataContext,
        CancellationToken ct)
    {
        var computed = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(configuration.ScenarioCode))
        {
            return computed;
        }

        var scenarioJson = await dbContext.Set<DocumentConstructorScenario>()
            .AsNoTracking()
            .Where(scenario => scenario.IsActive && scenario.Code == configuration.ScenarioCode)
            .Select(scenario => scenario.ScenarioJson)
            .FirstOrDefaultAsync(ct);

        if (scenarioJson is null)
        {
            return ErrorDetails.Validation(
                "DocGen.ScenarioNotFound",
                $"Scenario '{configuration.ScenarioCode}' was not found.");
        }

        ConstructorScenarioDefinition? scenario;
        try
        {
            scenario = JsonSerializer.Deserialize<ConstructorScenarioDefinition>(scenarioJson, JsonOptions);
        }
        catch (JsonException)
        {
            return ErrorDetails.Validation(
                "DocGen.InvalidScenario",
                $"Scenario '{configuration.ScenarioCode}' has invalid JSON.");
        }

        if (scenario is null)
        {
            return ErrorDetails.Validation(
                "DocGen.InvalidScenario",
                $"Scenario '{configuration.ScenarioCode}' has invalid JSON.");
        }

        var scenarioContext = new DocumentScenarioContext
        {
            ScenarioCode = configuration.ScenarioCode,
            Configuration = configuration,
            Parameters = parameters,
            DataContext = dataContext
        };

        foreach (var helperKey in scenario.HelperKeys)
        {
            if (!helpersByKey.TryGetValue(helperKey, out var helper))
            {
                return ErrorDetails.Validation(
                    "DocGen.ScenarioHelperNotRegistered",
                    $"Scenario helper '{helperKey}' is not registered.");
            }

            var helperResult = await helper.BuildAsync(scenarioContext, ct);
            if (helperResult.IsFailure)
            {
                return helperResult.ErrorDetails;
            }

            foreach (var (key, value) in helperResult.Value!)
            {
                computed[key] = value;
            }
        }

        return computed;
    }
}
