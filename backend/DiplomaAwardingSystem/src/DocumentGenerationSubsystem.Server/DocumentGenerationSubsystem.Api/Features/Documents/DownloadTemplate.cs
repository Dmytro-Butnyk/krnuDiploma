using Core.Api.Extensions;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using DocumentGenerationSubsystem.Api.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DocumentGenerationSubsystem.Api.Features.Documents;

public static class DownloadTemplate
{
    internal sealed class Endpoint
    {
        public static void MapEndpoints(IEndpointRouteBuilder app)
        {
            app.MapGet("/documents/templates/{id:int}/file", Handle)
                .WithSummary("Downloads the original .docx template file")
                .Produces(StatusCodes.Status200OK, contentType: "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithTags("DocumentGeneration");
        }

        private static async Task<Results<FileContentHttpResult, ProblemHttpResult>> Handle(
            [FromRoute] int id,
            [FromServices] Handler handler,
            CancellationToken ct)
        {
            var result = await handler.HandleAsync(id, ct);

            if (result.IsFailure) return result.ToProblemDetails();

            return TypedResults.File(
                fileContents: result.Value.Content,
                contentType: "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                fileDownloadName: $"{result.Value.FileName}.docx");
        }
    }

    private sealed class Handler(DbDocGenContext context) : IScopedService
    {
        public async Task<Result<(byte[] Content, string FileName)>> HandleAsync(int id, CancellationToken ct)
        {
            var template = await context.Set<DocumentTemplate>()
                .AsNoTracking()
                .Select(t => new { t.WordTemplate, t.Name, t.Id })
                .FirstOrDefaultAsync(t => t.Id == id, ct);

            if (template == null) return ErrorDetails.NotFound("Template.NotFound", "Template not found.");

            return (template.WordTemplate, template.Name);
        }
    }
}
