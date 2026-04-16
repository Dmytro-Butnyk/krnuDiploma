using System.Text.Json;
using Core.Infrastructure;
using DocumentGenerationSubsystem.Api.Entities;
using DocumentGenerationSubsystem.Api.Models;
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

        documentGenerationGroup.MapPost("uploadTemplate", UploadTemplate)
            .DisableAntiforgery()
            .WithSummary("Uploads template");
    }

    private static async Task<Results<Ok<string>, BadRequest<ProblemDetails>>> UploadTemplate(
        [AsParameters] UploadTemplateRequest request,
        [FromServices] DbDocGenContext dbContext,
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
        DocumentTemplate template = new DocumentTemplate(request.Name, fileBytes, request.ConfigurationJson);

        dbContext.Set<DocumentTemplate>().Add(template);
        await dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok("Template uploaded successfully.");
    }
}
