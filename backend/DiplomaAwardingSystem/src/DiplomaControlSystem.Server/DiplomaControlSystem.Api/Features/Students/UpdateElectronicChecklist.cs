using Core.Api.Extensions;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using DiplomaControlSystem.Api.Infrastructure.Access;
using DiplomaControlSystem.Api.Infrastructure.Students;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DiplomaControlSystem.Api.Features.Students;

public static class UpdateElectronicChecklist
{
    public sealed record Request(
        string SecretaryEmail,
        bool HasRegulatoryControl,
        bool HasExplanatoryNoteDoc,
        bool HasExplanatoryNotePdf,
        bool HasPlagiarismReportPdf,
        bool HasReviewDoc,
        bool HasPresentationPpt);

    public sealed record Response(
        int StudentId,
        bool HasRegulatoryControl,
        bool HasExplanatoryNoteDoc,
        bool HasExplanatoryNotePdf,
        bool HasPlagiarismReportPdf,
        bool HasReviewDoc,
        bool HasPresentationPpt);

    internal sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.SecretaryEmail)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(320);
        }
    }

    internal static class Endpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPatch("/students/{studentId:int}/electronic-checklist", Handle)
                .WithSummary("Updates student electronic components checklist")
                .Produces<Response>(StatusCodes.Status200OK)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithTags("Students");
        }

        private static async Task<Results<Ok<Response>, ProblemHttpResult, ValidationProblem>> Handle(
            [FromRoute] int studentId,
            [FromBody] Request request,
            [FromServices] IValidator<Request> validator,
            [FromServices] Handler handler,
            CancellationToken ct)
        {
            ValidationResult validationResult = await validator.ValidateAsync(request, ct);

            if (!validationResult.IsValid)
            {
                return TypedResults.ValidationProblem(validationResult.ToDictionary());
            }

            var result = await handler.HandleAsync(studentId, request, ct);

            if (result.IsFailure)
            {
                return result.ToProblemDetails();
            }

            return TypedResults.Ok(result.Value!);
        }
    }

    private sealed class Handler(
        DbDocGenContext context,
        StudentAccessService studentAccessService) : IScopedService
    {
        public async Task<Result<Response>> HandleAsync(
            int studentId,
            Request request,
            CancellationToken ct)
        {
            var accessResult = await studentAccessService.GetForSecretaryAsync(studentId, request.SecretaryEmail, ct);
            if (accessResult.IsFailure)
            {
                return accessResult.ErrorDetails;
            }

            var student = await context.Students
                .Include(s => s.ElectronicComponentsChecklist)
                .FirstOrDefaultAsync(s => s.Id == studentId, ct);

            if (student is null)
            {
                return ErrorDetails.NotFound(
                    "Student.NotFound",
                    "Student was not found.");
            }

            var checklist = StudentDiplomaDataInitializer.EnsureElectronicChecklist(student);
            checklist.HasRegulatoryControl = request.HasRegulatoryControl;
            checklist.HasExplanatoryNoteDoc = request.HasExplanatoryNoteDoc;
            checklist.HasExplanatoryNotePdf = request.HasExplanatoryNotePdf;
            checklist.HasPlagiarismReportPdf = request.HasPlagiarismReportPdf;
            checklist.HasReviewDoc = request.HasReviewDoc;
            checklist.HasPresentationPpt = request.HasPresentationPpt;

            await context.SaveChangesAsync(ct);

            return new Response(
                student.Id,
                checklist.HasRegulatoryControl,
                checklist.HasExplanatoryNoteDoc,
                checklist.HasExplanatoryNotePdf,
                checklist.HasPlagiarismReportPdf,
                checklist.HasReviewDoc,
                checklist.HasPresentationPpt);
        }
    }
}
