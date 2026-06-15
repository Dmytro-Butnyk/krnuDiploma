using System.Text.Json;
using Core.Infrastructure;
using DocumentGenerationSubsystem.Api.Entities.DocumentGeneration;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DocumentGenerationSubsystem.Api.Features.Constructor;

public static class GetConstructorScenarios
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    internal static class Endpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/constructor/scenarios", Handle)
                .WithSummary("Gets document data scenarios for the template constructor")
                .Produces<IReadOnlyCollection<ConstructorScenarioDefinition>>(StatusCodes.Status200OK)
                .WithTags("TemplateConstructor");
        }

        private static async Task<Ok<IReadOnlyCollection<ConstructorScenarioDefinition>>> Handle(
            [FromServices] DbDocGenContext context,
            [FromServices] ILoggerFactory loggerFactory,
            CancellationToken ct)
        {
            var logger = loggerFactory.CreateLogger(nameof(GetConstructorScenarios));
            var scenarioRows = await context.Set<DocumentConstructorScenario>()
                .AsNoTracking()
                .Where(scenario => scenario.IsActive)
                .OrderBy(scenario => scenario.Title)
                .Select(scenario => scenario.ScenarioJson)
                .ToListAsync(ct);

            var scenarios = new List<ConstructorScenarioDefinition>(scenarioRows.Count);
            foreach (var scenarioJson in scenarioRows)
            {
                try
                {
                    var scenario = JsonSerializer.Deserialize<ConstructorScenarioDefinition>(
                        scenarioJson,
                        JsonOptions);

                    if (scenario is not null)
                    {
                        scenarios.Add(scenario);
                    }
                }
                catch (JsonException ex)
                {
                    logger.LogError(ex, "Failed to parse document constructor scenario JSON.");
                }
            }

            return TypedResults.Ok<IReadOnlyCollection<ConstructorScenarioDefinition>>(scenarios);
        }
    }
}
