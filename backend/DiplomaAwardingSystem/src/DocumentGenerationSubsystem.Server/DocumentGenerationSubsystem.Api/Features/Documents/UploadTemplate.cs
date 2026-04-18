using System.Text.Json;
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

public static class UploadTemplate
{
    public record Request(string Name, string ConfigurationJson, IFormFile File);

    public record Response(string Name, int TemplateId);

    internal sealed class Validator : AbstractValidator<Request>
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
            
            RuleFor(x => x.File)
                .NotNull()
                .WithMessage("File cannot be null.")
                .DependentRules(() =>
                {
                    RuleFor(x => x.File.Length)
                        .GreaterThan(0)
                        .LessThanOrEqualTo(5_242_880) // 5 MB limit
                        .WithMessage("File size must be between 1 byte and 5 MB.");

                    RuleFor(x => x.File.FileName)
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
            app.MapPost("/templates", Handle)
                .DisableAntiforgery()
                .WithSummary("Uploads a new document template")
                .Produces<Response>(StatusCodes.Status201Created)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status409Conflict)
                .ProducesProblem(StatusCodes.Status500InternalServerError)
                .WithTags("DocumentGeneration");
        }
        
        private static async Task<Results<Created<Response>, ProblemHttpResult, ValidationProblem>> Handle(
            [FromForm] Request request,
            [FromServices] IValidator<Request> validator,
            [FromServices] Handler handler,
            CancellationToken ct)
        {
            ValidationResult validationResult = await validator.ValidateAsync(request, ct);
            
            if (!validationResult.IsValid)
            {
                return TypedResults.ValidationProblem(validationResult.ToDictionary());
            }
            
            var result = await handler.HandleAsync(request, ct);

            if (result.IsFailure)
            {
                return result.ToProblemDetails();
            }
            
            return TypedResults.Created($"/templates/{result.Value!.TemplateId}", result.Value!);
        }
    }
    
    internal sealed class Handler(
        DbDocGenContext context) : IScopedService
    {
        public async Task<Result<Response>> HandleAsync(
            Request request,
            CancellationToken ct)
        {
            bool nameExists = await context.Set<DocumentTemplate>()
                .AnyAsync(t => t.Name == request.Name, ct);
                
            if (nameExists)
            {
                return ErrorDetails.Conflict(
                    "DocGen.TemplateNameNotUnique", 
                    "A template with this name already exists.");
            }
            
            using var memoryStream = new MemoryStream();
            await request.File.CopyToAsync(memoryStream, ct);
            var fileBytes = memoryStream.ToArray();

            DocumentTemplate template = new DocumentTemplate(request.Name, fileBytes, request.ConfigurationJson);

            await context.Set<DocumentTemplate>().AddAsync(template, ct);
            await context.SaveChangesAsync(ct);
            
            return new Response(template.Name, template.Id);
        }
    }
}
