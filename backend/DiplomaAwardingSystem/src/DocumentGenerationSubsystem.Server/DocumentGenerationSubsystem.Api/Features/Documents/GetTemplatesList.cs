using Core.Domain.DependencyInjectionInterfaces;
using Core.Infrastructure;
using DocumentGenerationSubsystem.Api.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DocumentGenerationSubsystem.Api.Features.Documents;

public static class GetTemplatesList
{
    public record TemplateListItemDto(int Id, string Name);

    internal sealed class Endpoint
    {
        public static void MapEndpoints(IEndpointRouteBuilder app)
        {
            app.MapGet("/documents/templates", Handle)
                .WithSummary("Gets a lightweight list of all document templates")
                .Produces<IReadOnlyCollection<TemplateListItemDto>>(StatusCodes.Status200OK)
                .WithTags("DocumentGeneration");
        }

        private static async Task<Ok<List<TemplateListItemDto>>> Handle(
            [FromServices] Handler handler,
            CancellationToken ct)
        {
            var result = await handler.HandleAsync(ct);
            return TypedResults.Ok(result);
        }
    }

    private sealed class Handler(DbDocGenContext context) : IScopedService
    {
        public async Task<List<TemplateListItemDto>> HandleAsync(CancellationToken ct)
        {
            return await context.Set<DocumentTemplate>()
                .AsNoTracking()
                .Select(dt => new TemplateListItemDto(dt.Id, dt.Name))
                .ToListAsync(ct);
        }
    }
}
