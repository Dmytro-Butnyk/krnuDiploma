using System.Text.Json;
using Core.Api.Extensions;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using DocumentGenerationSubsystem.Api.Entities;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DocumentGenerationSubsystem.Api.Features.Documents;

public static class UpdateTemplate
{
    public record UpdateTemplateRequest(int TemplateId, string? Name, IFormFile? Template, string? ConfigurationJson);

    internal sealed class Validator : AbstractValidator<UpdateTemplateRequest>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .MaximumLength(255)
                .When(x => x.Name != null);

            RuleFor(x => x.ConfigurationJson)
                .Must(BeValidJson!)
                .WithMessage("Configuration must be a valid JSON format.")
                .When(x => x.ConfigurationJson != null);

            RuleFor(x => x.Template)
                .Must(x => x!.Length > 0)
                .WithMessage("Template cannot be empty.")
                .Must(x => x!.Length <= 5_242_880)
                .WithMessage("Template size must be less than 5 MB.")
                .Must(x => x!.FileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                .WithMessage("Only .docx files are allowed.")
                .When(x => x.Template != null);
        }

        private static bool BeValidJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return false;
            try
            {
                using var doc = JsonDocument.Parse(json);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }

    internal static class Endpoint
    {
        public static void MapEndpoints(IEndpointRouteBuilder app)
        {
            app.MapPatch("/documents/templates/{id:int}", Handle)
                .DisableAntiforgery()
                .WithSummary("Updates a document template")
                .Produces<int>(StatusCodes.Status200OK)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithTags("DocumentGeneration");
        }

        private static async Task<Results<Ok<int>, ProblemHttpResult, ValidationProblem>> Handle(
            [FromRoute] int id,
            [FromForm] UpdateTemplateRequest request,
            [FromServices] IValidator<UpdateTemplateRequest> validator,
            [FromServices] Handler handler,
            CancellationToken ct)
        {
            var finalRequest = request with { TemplateId = id };

            ValidationResult validationResult = await validator.ValidateAsync(finalRequest, ct);

            if (!validationResult.IsValid)
            {
                return TypedResults.ValidationProblem(validationResult.ToDictionary());
            }

            var result = await handler.HandleAsync(finalRequest, ct);

            if (result.IsFailure)
            {
                return result.ToProblemDetails();
            }

            return TypedResults.Ok(id);
        }
    }

    private sealed class Handler(DbDocGenContext context) : IScopedService
    {
        public async Task<Result> HandleAsync(UpdateTemplateRequest request, CancellationToken ct)
        {
            var template = await context.Set<DocumentTemplate>()
                .FirstOrDefaultAsync(dt => dt.Id == request.TemplateId, ct);

            if (template == null)
            {
                return ErrorDetails.NotFound("Template.NotFound", "Template not found.");
            }

            if (!string.IsNullOrWhiteSpace(request.Name) && request.Name != template.Name)
            {
                bool nameExists = await context.Set<DocumentTemplate>()
                    .AnyAsync(dt => dt.Name == request.Name && dt.Id != request.TemplateId, ct);

                if (nameExists)
                {
                    return ErrorDetails.Conflict(
                        "Template.DuplicateName",
                        $"Template with name '{request.Name}' already exists.");
                }

                request.Name.UpdateIfNotNull(v => template.Name = v);
            }

            request.ConfigurationJson.UpdateIfNotNull(v => template.ConfigurationJson = v);

            if (request.Template != null)
            {
                try
                {
                    using var ms = new MemoryStream();
                    await request.Template.CopyToAsync(ms, ct);
                    template.WordTemplate = ms.ToArray();
                }
                catch (Exception)
                {
                    return ErrorDetails.Conflict(
                        "Template.UploadError",
                        "Error processing file.");
                }
            }

            await context.SaveChangesAsync(ct);
            return Result.Success();
        }
    }
}
