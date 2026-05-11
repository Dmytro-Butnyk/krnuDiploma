using Core.Api.Extensions;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using DocumentGenerationSubsystem.Api.Entities;
using DocumentGenerationSubsystem.Api.Infrastructure.Engines;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DocumentGenerationSubsystem.Api.Features.Documents;

internal static class GenerateDocument
{
    // --------------------------------------------------------------------------
    // 1. КОНТРАКТЫ (DTO)
    // --------------------------------------------------------------------------
    internal sealed record GenerateDocumentRequest(Dictionary<string, string> Parameters);

    // --------------------------------------------------------------------------
    // 2. ВАЛИДАЦИЯ
    // --------------------------------------------------------------------------
    internal sealed class Validator : AbstractValidator<GenerateDocumentRequest>
    {
        public Validator()
        {
            RuleFor(x => x.Parameters)
                .NotNull()
                .WithMessage("Parameters dictionary cannot be null.");
        }
    }

    // --------------------------------------------------------------------------
    // 3. МАРШРУТИЗАЦИЯ И ОРКЕСТРАЦИЯ
    // --------------------------------------------------------------------------
    internal static class Endpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/documents/{id:int:min(1)}/generate", Handle)
                .WithSummary("Generates document from template")
                .Produces<FileStreamHttpResult>(StatusCodes.Status200OK, "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status500InternalServerError)
                .WithTags("DocumentGeneration");
        }

        private static async Task<Results<FileStreamHttpResult, ProblemHttpResult, ValidationProblem>> Handle(
            [FromRoute] int id,
            [FromBody] GenerateDocumentRequest generateDocumentRequest,
            [FromServices] IValidator<GenerateDocumentRequest> validator,
            [FromServices] Handler handler,
            CancellationToken ct)
        {
            // 1. Fail-fast валидация
            ValidationResult validationResult = await validator.ValidateAsync(generateDocumentRequest, ct);
            if (!validationResult.IsValid)
            {
                return TypedResults.ValidationProblem(validationResult.ToDictionary());
            }

            // 2. Вызов бизнес-логики
            var result = await handler.HandleAsync(id, generateDocumentRequest, ct);

            // 3. Fail-fast проверка на ошибки домена/БД
            if (result.IsFailure)
            {
                // ToProblemDetails() автоматически определит статус код по ErrorType
                return result.ToProblemDetails();
            }

            // 4. Успешный результат
            var document = result.Value!;
            
            return TypedResults.Stream(
                stream: document.Stream,
                contentType: "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                fileDownloadName: document.FileName);
        }
    }

    // --------------------------------------------------------------------------
    // 4. БИЗНЕС-ЛОГИКА (Handler)
    // --------------------------------------------------------------------------
    private sealed class Handler(
        DbDocGenContext context,
        DynamicDocumentEngine documentEngine) : IScopedService
    {
        public async Task<Result<(Stream Stream, string FileName)>> HandleAsync(
            int templateId,
            GenerateDocumentRequest generateDocumentRequest,
            CancellationToken cancellationToken)
        {
            DocumentTemplate? template = await context.Set<DocumentTemplate>()
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken);

            if (template is null)
            {
                return ErrorDetails.NotFound(
                    "DocGen.TemplateNotFound",
                    $"Template with ID {templateId} not found.");
            }

            Result<Stream> documentStreamResult = await documentEngine.GenerateAsync(
                template.ConfigurationJson,
                template.WordTemplate,
                generateDocumentRequest.Parameters,
                cancellationToken);

            if (documentStreamResult.IsFailure)
            {
                return documentStreamResult.ErrorDetails;
            }

            if (documentStreamResult.Value!.CanSeek)
            {
                documentStreamResult.Value!.Position = 0;
            }

            string fileName = $"{template.Name}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.docx";

            return (documentStreamResult.Value!, fileName);
        }
    }
}
