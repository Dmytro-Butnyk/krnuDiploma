using System.Text.Json;
using Core.Api.Extensions;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using DocumentGenerationSubsystem.Api.Entities;
using DocumentGenerationSubsystem.Api.Infrastructure.Configuration;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DocumentGenerationSubsystem.Api.Features.Documents;

public static class UploadTemplate
{
    public record UploadTemplateRequest(string Name, string ConfigurationJson, IFormFile Template);

    public record UploadTemplateResponse(string Name, int TemplateId);

    internal sealed class Validator : AbstractValidator<UploadTemplateRequest>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(255)
                .WithMessage("Name is required and cannot exceed 255 characters.");
            
            RuleFor(x => x.ConfigurationJson)
                .NotEmpty()
                .Must(BeValidJson)
                .WithMessage("Configuration must be a valid JSON format.");
            
            RuleFor(x => x.Template)
                .NotNull()
                .WithMessage("Template cannot be null.")
                .DependentRules(() =>
                {
                    RuleFor(x => x.Template.Length)
                        .GreaterThan(0)
                        .LessThanOrEqualTo(5_242_880) // 5 MB limit
                        .WithMessage("Template size must be between 1 byte and 5 MB.");

                    RuleFor(x => x.Template.FileName)
                        .Must(name => name.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                        .WithMessage("Only .docx files are allowed.");
                });
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
        public static void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/documents/templates", Handle)
                .DisableAntiforgery()
                .WithSummary("Uploads a new document template")
                .Produces<UploadTemplateResponse>(StatusCodes.Status201Created)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status409Conflict)
                .ProducesProblem(StatusCodes.Status500InternalServerError)
                .WithTags("DocumentGeneration");
        }
        
        private static async Task<Results<Created<UploadTemplateResponse>, ProblemHttpResult, ValidationProblem>> Handle(
            [FromForm] UploadTemplateRequest uploadTemplateRequest,
            [FromServices] IValidator<UploadTemplateRequest> validator,
            [FromServices] Handler handler,
            CancellationToken ct)
        {
            ValidationResult validationResult = await validator.ValidateAsync(uploadTemplateRequest, ct);
            
            if (!validationResult.IsValid)
            {
                return TypedResults.ValidationProblem(validationResult.ToDictionary());
            }
            
            var result = await handler.HandleAsync(uploadTemplateRequest, ct);

            if (result.IsFailure)
            {
                return result.ToProblemDetails();
            }
            
            return TypedResults.Created($"/templates/{result.Value!.TemplateId}", result.Value!);
        }
    }
    
    private sealed class Handler(
        DbDocGenContext context) : IScopedService
    {
        public async Task<Result<UploadTemplateResponse>> HandleAsync(
            UploadTemplateRequest uploadTemplateRequest,
            CancellationToken ct)
        {
            bool nameExists = await context.Set<DocumentTemplate>()
                .AnyAsync(t => t.Name == uploadTemplateRequest.Name, ct);
                
            if (nameExists)
            {
                return ErrorDetails.Conflict(
                    "DocGen.TemplateNameNotUnique", 
                    "A template with this name already exists.");
            }
            
            using var memoryStream = new MemoryStream();
            await uploadTemplateRequest.Template.CopyToAsync(memoryStream, ct);
            var fileBytes = memoryStream.ToArray();

            var configurationResult = TemplateConfigurationReader.Parse(uploadTemplateRequest.ConfigurationJson);
            if (configurationResult.IsFailure)
            {
                return configurationResult.ErrorDetails;
            }

            DocumentTemplate template = new DocumentTemplate(uploadTemplateRequest.Name, fileBytes, uploadTemplateRequest.ConfigurationJson);

            await context.Set<DocumentTemplate>().AddAsync(template, ct);
            await context.SaveChangesAsync(ct);
            
            return new UploadTemplateResponse(template.Name, template.Id);
        }
    }
}
