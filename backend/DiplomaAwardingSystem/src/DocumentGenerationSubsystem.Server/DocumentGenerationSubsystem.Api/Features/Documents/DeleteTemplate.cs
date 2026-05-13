using Core.Api.Extensions;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using DocumentGenerationSubsystem.Api.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DocumentGenerationSubsystem.Api.Features.Documents;

public static class DeleteTemplate
{
    internal static class Endpoint
    {
        public static void MapEndpoints(IEndpointRouteBuilder app)
        {
            app.MapDelete("/documents/templates/{id:int}", Handle)
                .WithSummary("Deletes a document template")
                .Produces<int>()
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithTags("DocumentGeneration");
        }

        private static async Task<Results<Ok<int>, ProblemHttpResult>> Handle(
            [FromRoute] int id,
            [FromServices] Handler handler,
            CancellationToken ct)
        {
            var result = await handler.HandleAsync(id, ct);

            if (result.IsFailure)
            {
                return result.ToProblemDetails();
            }

            return TypedResults.Ok(id);
        }
    }

    private sealed class Handler(DbDocGenContext context) : IScopedService
    {
        public async Task<Result> HandleAsync(int templateId, CancellationToken ct)
        {
            var template = await context.Set<DocumentTemplate>()
                .FirstOrDefaultAsync(dt => dt.Id == templateId, ct);

            if (template == null)
            {
                return ErrorDetails.NotFound("Template.NotFound", "Template not found.");
            }

            context.Set<DocumentTemplate>().Remove(template);
            await context.SaveChangesAsync(ct);
            
            return Result.Success();
        }
    }
}
