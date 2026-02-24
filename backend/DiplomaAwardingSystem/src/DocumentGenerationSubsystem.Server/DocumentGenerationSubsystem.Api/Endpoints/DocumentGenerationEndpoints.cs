using System.Text.Json;
using DocumentGenerationSubsystem.Application.Dto;
using DocumentGenerationSubsystem.Application.Handlers;
using DocumentGenerationSubsystem.Application.Interfaces;
using DocumentGenerationSubsystem.Domain.Entities;
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

        documentGenerationGroup.MapPost("", GenerateDocument)
            .WithSummary("Generates document");

        documentGenerationGroup.MapPost("uploadTemplate", UploadTemplate)
            .DisableAntiforgery()
            .WithSummary("Uploads template");
    }

    private static async Task<Results<FileStreamHttpResult, BadRequest<ProblemDetails>, NotFound<ProblemDetails>>> GenerateDocument(
        [FromBody] GenerateDocumentRequest request,
        [FromServices] GenerateDocumentHandler handler,
        CancellationToken ct)
    {
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

    private static async Task<Results<Ok<string>, BadRequest<ProblemDetails>>> UploadTemplate(
        [AsParameters] UploadTemplateRequest request,
        [FromServices] IDbDocGenContext dbContext,
        CancellationToken ct)
    {
        try
        {
            JsonDocument.Parse(request.ConfigurationJson);
        }
        catch (JsonException)
        {
            return TypedResults.BadRequest(
                new ProblemDetails { Detail = "Wrong configuration format." });
        }

        // 2. Читаем файл из потока HTTP-запроса
        if (request.File.Length == 0 || !request.File.FileName.EndsWith(".docx", StringComparison.Ordinal))
        {
            return TypedResults.BadRequest(
                new ProblemDetails { Detail = "Wrong configuration format." });
        }

        using var memoryStream = new MemoryStream();
        await request.File.CopyToAsync(memoryStream, ct);
        var fileBytes = memoryStream.ToArray();

        // 3. Создаем доменную сущность и сохраняем в БД
        var template = new DocumentTemplate(request.Name, fileBytes, request.ConfigurationJson);
    
        dbContext.DocumentTemplates.Add(template);
        await dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok("Template uploaded successfully.");
    } 
}
