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
    public sealed record UpdateElectronicChecklistRequest(
        bool HasRegulatoryControl,
        bool HasExplanatoryNoteDoc,
        bool HasExplanatoryNotePdf,
        bool HasPlagiarismReportPdf,
        bool HasReviewDoc,
        bool HasPresentationPpt);

    public sealed record UpdateElectronicChecklistResponse(
        int StudentId,
        bool HasRegulatoryControl,
        bool HasExplanatoryNoteDoc,
        bool HasExplanatoryNotePdf,
        bool HasPlagiarismReportPdf,
        bool HasReviewDoc,
        bool HasPresentationPpt);

    internal sealed class Validator : AbstractValidator<UpdateElectronicChecklistRequest>
    {
        public Validator()
        {
        }
    }

    internal static class Endpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPatch("/students/{studentId:int}/electronic-checklist", Handle)
                .WithSummary("Updates student electronic components checklist")
                .Produces<UpdateElectronicChecklistResponse>(StatusCodes.Status200OK)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithTags("Students");
        }

        private static async Task<Results<Ok<UpdateElectronicChecklistResponse>, ProblemHttpResult, ValidationProblem>> Handle(
            [FromRoute] int studentId,
            [FromBody] UpdateElectronicChecklistRequest request,
            [FromServices] IValidator<UpdateElectronicChecklistRequest> validator,
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
        public async Task<Result<UpdateElectronicChecklistResponse>> HandleAsync(
            int studentId,
            UpdateElectronicChecklistRequest request,
            CancellationToken ct)
        {
            var accessResult = await studentAccessService.GetForCurrentSecretaryAsync(studentId, ct);
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

            return new UpdateElectronicChecklistResponse(
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
