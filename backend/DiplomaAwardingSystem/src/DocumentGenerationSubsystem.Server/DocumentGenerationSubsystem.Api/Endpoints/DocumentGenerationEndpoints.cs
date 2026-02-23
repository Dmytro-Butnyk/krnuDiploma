using DocumentGenerationSubsystem.Application.Dto;
using DocumentGenerationSubsystem.Application.Handlers;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

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

    private static async Task<Results<FileStreamHttpResult, BadRequest<ProblemDetails>, NotFound<ProblemDetails>>> GenerateDocument(
        [FromBody] GenerateDocumentRequest request,
        [FromServices] GenerateDocumentHandler handler,
        CancellationToken ct)
    {
        // Вызываем бизнес-логику
        var result = await handler.HandleAsync(request, ct);

        return result switch
        {
            { IsSuccess: true, Value: var document } => TypedResults.Stream(
                stream: document.Stream,
                contentType: "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                fileDownloadName: document.FileName),
            { ErrorDetails.Code: "NotFound" } => TypedResults.NotFound(
                new ProblemDetails { Detail = result.ErrorDetails.Message }),
            
            _ => TypedResults.BadRequest(
                new ProblemDetails { Detail = result.ErrorDetails.Message })
        };
    }
}


