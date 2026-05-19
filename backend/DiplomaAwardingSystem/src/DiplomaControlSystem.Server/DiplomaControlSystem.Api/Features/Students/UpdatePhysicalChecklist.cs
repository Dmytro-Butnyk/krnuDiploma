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

public static class UpdatePhysicalChecklist
{
    public sealed record Request(
        string SecretaryEmail,
        bool HasStudentCard,
        bool HasGradeBook,
        bool HasCircular,
        bool HasSignedReview,
        bool HasCopyOfBankReceipt,
        bool HasExplanatoryNote);

    public sealed record Response(
        int StudentId,
        bool HasStudentCard,
        bool HasGradeBook,
        bool HasCircular,
        bool HasSignedReview,
        bool HasCopyOfBankReceipt,
        bool HasExplanatoryNote);

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
            app.MapPatch("/students/{studentId:int}/physical-checklist", Handle)
                .WithSummary("Updates student physical components checklist")
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
                .Include(s => s.PhysicalComponentsChecklist)
                .FirstOrDefaultAsync(s => s.Id == studentId, ct);

            if (student is null)
            {
                return ErrorDetails.NotFound(
                    "Student.NotFound",
                    "Student was not found.");
            }

            var checklist = StudentDiplomaDataInitializer.EnsurePhysicalChecklist(student);
            checklist.HasStudentCard = request.HasStudentCard;
            checklist.HasGradeBook = request.HasGradeBook;
            checklist.HasCircular = request.HasCircular;
            checklist.HasSignedReview = request.HasSignedReview;
            checklist.HasCopyOfBankReceipt = request.HasCopyOfBankReceipt;
            checklist.HasExplanatoryNote = request.HasExplanatoryNote;

            await context.SaveChangesAsync(ct);

            return new Response(
                student.Id,
                checklist.HasStudentCard,
                checklist.HasGradeBook,
                checklist.HasCircular,
                checklist.HasSignedReview,
                checklist.HasCopyOfBankReceipt,
                checklist.HasExplanatoryNote);
        }
    }
}
