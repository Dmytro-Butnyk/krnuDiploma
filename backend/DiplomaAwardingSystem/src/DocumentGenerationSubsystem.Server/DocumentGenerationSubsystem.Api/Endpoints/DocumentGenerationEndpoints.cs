using Microsoft.AspNetCore.Http.HttpResults;

#pragma warning disable SA1122

namespace DocumentGenerationSubsystem.Api.Endpoints;

public static class DocumentGenerationEndpoints
{
    private const string Route = "api/docGen";

    public static void MapDocumentGenerationEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder documentGenerationGroup = app.MapGroup(Route)
            .WithTags("DocumentGeneration");
        
        MapGenerationEndpoints(documentGenerationGroup);
    }

    private static void MapGenerationEndpoints(RouteGroupBuilder group)
    {
        group.MapPost("", GenerateDocument)
            .WithSummary("Generates document");
    }

    private static async Task<Results<NoContent, BadRequest<string>>> GenerateDocument(
        CancellationToken ct)
    {
        await Task.Delay(1, ct);
        return TypedResults.NoContent();
    }
}
