using System.IO.Compression;
using System.Text.RegularExpressions;
using Core.Api.Extensions;
using Core.Domain.ResultPattern;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace DocumentGenerationSubsystem.Api.Features.Documents;

public static class ScanTemplateForTags
{
    public record ScanTemplateForTagsRequest(IFormFile Template);

    public record ScanTemplateForTagsResponse(IReadOnlyList<string> Tags);

    internal sealed class Validator : AbstractValidator<ScanTemplateForTagsRequest>
    {
        public Validator()
        {
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
    }

    internal static class Endpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/documents/scan", Handle)
                .DisableAntiforgery()
                .WithSummary("Scans a document template for tags")
                .Produces<ScanTemplateForTagsResponse>()
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status409Conflict)
                .ProducesProblem(StatusCodes.Status500InternalServerError)
                .WithTags("DocumentGeneration");
        }

        private static async Task<Results<Ok<ScanTemplateForTagsResponse>, ProblemHttpResult, ValidationProblem>> Handle(
            [FromForm] ScanTemplateForTagsRequest scanTemplateForTagsRequest,
            [FromServices] IValidator<ScanTemplateForTagsRequest> validator,
            CancellationToken ct)
        {
            ValidationResult validationResult = await validator.ValidateAsync(scanTemplateForTagsRequest, ct);

            if (!validationResult.IsValid)
            {
                return TypedResults.ValidationProblem(validationResult.ToDictionary());
            }
            
            var result = await Handler.HandleAsync(scanTemplateForTagsRequest, ct);

            if (result.IsFailure)
            {
                return result.ToProblemDetails();
            }

            return TypedResults.Ok(result.Value!);
        }
    }

    private static class Handler
    {
        public static async Task<Result<ScanTemplateForTagsResponse>> HandleAsync(
            ScanTemplateForTagsRequest scanTemplateForTagsRequest,
            CancellationToken ct)
        {
            try
            {
                using var memoryStream = new MemoryStream();
                await scanTemplateForTagsRequest.Template.CopyToAsync(memoryStream, ct);
                byte[] fileBytes = memoryStream.ToArray();

                var tags = ExtractTagsFromDocx(fileBytes);

                return Result.Success(new ScanTemplateForTagsResponse(tags));
            }
            catch (Exception)
            {
                return ErrorDetails.Conflict(
                    "DocGen.Template.InvalidFormat", 
                    "The provided file is not a valid or readable Word document.");
            }
        }
        
        private static List<string> ExtractTagsFromDocx(byte[] fileBytes)
        {
            var tags = new HashSet<string>(); 
    
            using var ms = new MemoryStream(fileBytes);
            using var archive = new ZipArchive(ms, ZipArchiveMode.Read);
    
            foreach (var entry in archive.Entries)
            {
                if (entry.FullName.StartsWith(
                        "word/",
                        StringComparison.OrdinalIgnoreCase)
                    && entry.FullName.EndsWith(
                        ".xml", StringComparison.OrdinalIgnoreCase))
                {
                    using var stream = entry.Open();
                    using var reader = new StreamReader(stream);
                    var xmlContent = reader.ReadToEnd();
            
                    // Looking for {{Tag}} directly in raw XML.
                    // [^<>{}] means: inside double curly braces there can be any text,
                    // EXCEPT < and > characters (a sign that an XML tag has crept in) and other curly braces.
                    var matches = Regex.Matches(xmlContent, @"\{\{([^<>{}]+)\}\}");
            
                    foreach (Match match in matches)
                    {
                        tags.Add(match.Groups[1].Value.Trim()); 
                    }
                }
            }

            return tags.ToList();
        }
    }
}
