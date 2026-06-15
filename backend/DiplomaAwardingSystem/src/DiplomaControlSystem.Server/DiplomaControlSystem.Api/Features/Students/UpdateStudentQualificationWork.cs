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

public static class UpdateStudentQualificationWork
{
    public sealed record UpdateStudentQualificationWorkRequest(
        string Topic,
        int? SupervisorId,
        string PracticeBase,
        int? ReviewerId);

    public sealed record UpdateStudentQualificationWorkResponse(
        int StudentId,
        string Topic,
        int? SupervisorId,
        string PracticeBase,
        int? ReviewerId);

    internal sealed class Validator : AbstractValidator<UpdateStudentQualificationWorkRequest>
    {
        public Validator()
        {
            RuleFor(x => x.Topic)
                .NotNull()
                .MaximumLength(500);

            RuleFor(x => x.PracticeBase)
                .NotNull()
                .MaximumLength(256);

            RuleFor(x => x)
                .Must(x => x.SupervisorId is null || x.SupervisorId != x.ReviewerId)
                .WithMessage("Reviewer cannot be the same teacher as supervisor.");
        }
    }

    internal static class Endpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPatch("/students/{studentId:int}/qualification-work", Handle)
                .WithSummary("Updates student qualification work base data")
                .Produces<UpdateStudentQualificationWorkResponse>(StatusCodes.Status200OK)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithTags("Students");
        }

        private static async Task<Results<Ok<UpdateStudentQualificationWorkResponse>, ProblemHttpResult, ValidationProblem>> Handle(
            [FromRoute] int studentId,
            [FromBody] UpdateStudentQualificationWorkRequest request,
            [FromServices] IValidator<UpdateStudentQualificationWorkRequest> validator,
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
        public async Task<Result<UpdateStudentQualificationWorkResponse>> HandleAsync(
            int studentId,
            UpdateStudentQualificationWorkRequest request,
            CancellationToken ct)
        {
            var accessResult = await studentAccessService.GetForCurrentSecretaryAsync(studentId, ct);
            if (accessResult.IsFailure)
            {
                return accessResult.ErrorDetails;
            }

            var teacherValidationResult = await ValidateTeachersAsync(request, ct);
            if (teacherValidationResult.IsFailure)
            {
                return teacherValidationResult.ErrorDetails;
            }

            var student = await context.Students
                .Include(s => s.QualificationWork)
                .FirstOrDefaultAsync(s => s.Id == studentId, ct);

            if (student is null)
            {
                return ErrorDetails.NotFound(
                    "Student.NotFound",
                    "Student was not found.");
            }

            var qualificationWork = StudentDiplomaDataInitializer.EnsureQualificationWork(student);
            qualificationWork.Topic = request.Topic.Trim();
            qualificationWork.TeacherId = request.SupervisorId;
            qualificationWork.PracticeBase = request.PracticeBase.Trim();
            qualificationWork.ReviewerId = request.ReviewerId;

            await context.SaveChangesAsync(ct);

            return new UpdateStudentQualificationWorkResponse(
                student.Id,
                qualificationWork.Topic,
                qualificationWork.TeacherId,
                qualificationWork.PracticeBase,
                qualificationWork.ReviewerId);
        }

        private async Task<Result> ValidateTeachersAsync(
            UpdateStudentQualificationWorkRequest request,
            CancellationToken ct)
        {
            if (request.SupervisorId is not null)
            {
                var supervisorExists = await context.Teachers
                    .AnyAsync(
                        t => t.Id == request.SupervisorId && t.IsActive,
                        ct);

                if (!supervisorExists)
                {
                    return ErrorDetails.Validation(
                        "QualificationWork.SupervisorInvalid",
                        "Supervisor must be an active teacher.");
                }
            }

            if (request.ReviewerId is not null)
            {
                var reviewerExists = await context.Teachers
                    .AnyAsync(
                        t => t.Id == request.ReviewerId && t.IsActive,
                        ct);

                if (!reviewerExists)
                {
                    return ErrorDetails.Validation(
                        "QualificationWork.ReviewerInvalid",
                        "Reviewer must be an active teacher.");
                }
            }

            return Result.Success();
        }
    }
}
