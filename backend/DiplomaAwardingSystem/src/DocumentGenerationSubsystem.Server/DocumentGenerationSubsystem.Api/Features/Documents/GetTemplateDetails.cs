using System.Text.Json;
using Core.Api.Extensions;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using DocumentGenerationSubsystem.Api.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DocumentGenerationSubsystem.Api.Features.Documents;

public static class GetTemplateDetails
{
    public record GetTemplateDetailsResponse(int Id, string Name, string? ConfigurationJson, IReadOnlyCollection<string> RequiredArguments);

    internal sealed class Endpoint
    {
        public static void MapEndpoints(IEndpointRouteBuilder app)
        {
            app.MapGet("/documents/templates/{id:int}", Handle)
                .WithSummary("Gets template details with required arguments for form generation")
                .Produces<GetTemplateDetailsResponse>()
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status409Conflict)
                .ProducesProblem(StatusCodes.Status500InternalServerError)
                .WithTags("DocumentGeneration");
        }

        private static async Task<Results<Ok<GetTemplateDetailsResponse>, ProblemHttpResult>> Handle(
            [FromRoute] int id,
            [FromServices] Handler handler,
            CancellationToken ct)
        {
            var result = await handler.HandleAsync(id, ct);

            if (result.IsFailure)
            {
                return result.ToProblemDetails();
            }

            return TypedResults.Ok(result.Value);
        }
    }

    private sealed class Handler(DbDocGenContext context, ILogger<Handler> logger) : IScopedService
    {
        public async Task<Result<GetTemplateDetailsResponse>> HandleAsync(int id, CancellationToken ct)
        {
            var template = await context.Set<DocumentTemplate>()
                .AsNoTracking()
                .FirstOrDefaultAsync(dt => dt.Id == id, ct);

            if (template == null)
            {
                return ErrorDetails.NotFound("Template.NotFound", "Template not found.");
            }

            var argsResult = ExtractFilterArgs(template.ConfigurationJson);
            
            if (argsResult.IsFailure)
            {
                logger.LogError("Corrupted ConfigurationJson in TemplateId: {TemplateId}", id);
                return ErrorDetails.Conflict("Template.CorruptedData", "Template configuration is invalid or corrupted.");
            }

            return new GetTemplateDetailsResponse(
                template.Id,
                template.Name,
                template.ConfigurationJson,
                argsResult.Value!);
        }

        private static Result<List<string>> ExtractFilterArgs(string? json)
        {
            var args = new List<string>();
            
            if (string.IsNullOrWhiteSpace(json)) 
                return args;

            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("DataSources", out var dataSources))
                {
                    foreach (var source in dataSources.EnumerateArray())
                    {
                        if (source.TryGetProperty("FilterArgs", out var filterArgs))
                        {
                            foreach (var arg in filterArgs.EnumerateArray())
                            {
                                var argName = arg.GetString();
                                if (!string.IsNullOrEmpty(argName)) args.Add(argName);
                            }
                        }
                    }
                }
                
                return args.Distinct().ToList();
            }
            catch (JsonException)
            {
                return ErrorDetails.Validation("Json.ParseError", "Failed to parse template configuration.");
            }
            catch (Exception)
            {
                return ErrorDetails.Conflict("Json.UnknownError", "An unexpected error occurred while reading configuration.");
            }
        }
    }
}
