using Core.Api.Extensions;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using DocumentGenerationSubsystem.Api.Entities;
using DocumentGenerationSubsystem.Api.Infrastructure.Configuration;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DocumentGenerationSubsystem.Api.Features.Documents;

public static class GetTemplateGenerationForm
{
    internal static class Endpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/documents/templates/{id:int}/generation-form", Handle)
                .WithSummary("Gets generation form metadata for a document template")
                .Produces<GenerationFormDto>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status409Conflict)
                .WithTags("DocumentGeneration");
        }

        private static async Task<Results<Ok<GenerationFormDto>, ProblemHttpResult>> Handle(
            [FromRoute] int id,
            [FromServices] Handler handler,
            CancellationToken ct)
        {
            var result = await handler.HandleAsync(id, ct);

            return result.IsFailure
                ? result.ToProblemDetails()
                : TypedResults.Ok(result.Value!);
        }
    }

    private sealed class Handler(DbDocGenContext context) : IScopedService
    {
        public async Task<Result<GenerationFormDto>> HandleAsync(int templateId, CancellationToken ct)
        {
            var template = await context.Set<DocumentTemplate>()
                .AsNoTracking()
                .Where(t => t.Id == templateId)
                .Select(t => new { t.Id, t.ConfigurationJson })
                .FirstOrDefaultAsync(ct);

            if (template is null)
            {
                return ErrorDetails.NotFound("Template.NotFound", "Template not found.");
            }

            var configResult = TemplateConfigurationReader.Parse(template.ConfigurationJson);
            if (configResult.IsFailure)
            {
                return ErrorDetails.Conflict(
                    "Template.CorruptedData",
                    "Template configuration is invalid or corrupted.");
            }

            return GenerationFormMapper.Map(template.Id, configResult.Value!);
        }
    }
}
