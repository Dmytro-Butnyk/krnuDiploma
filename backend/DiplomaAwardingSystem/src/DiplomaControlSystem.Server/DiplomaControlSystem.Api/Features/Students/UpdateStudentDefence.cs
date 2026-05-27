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

public static class UpdateStudentDefence
{
    public sealed record UpdateStudentDefenceRequest(DateOnly? DefenceDate);

    public sealed record UpdateStudentDefenceResponse(int StudentId, DateOnly? DefenceDate);

    internal sealed class Validator : AbstractValidator<UpdateStudentDefenceRequest>
    {
        public Validator()
        {
        }
    }

    internal static class Endpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPatch("/students/{studentId:int}/defence", Handle)
                .WithSummary("Updates student defence information")
                .Produces<UpdateStudentDefenceResponse>(StatusCodes.Status200OK)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithTags("Students");
        }

        private static async Task<Results<Ok<UpdateStudentDefenceResponse>, ProblemHttpResult, ValidationProblem>> Handle(
            [FromRoute] int studentId,
            [FromBody] UpdateStudentDefenceRequest request,
            [FromServices] IValidator<UpdateStudentDefenceRequest> validator,
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
        public async Task<Result<UpdateStudentDefenceResponse>> HandleAsync(
            int studentId,
            UpdateStudentDefenceRequest request,
            CancellationToken ct)
        {
            var accessResult = await studentAccessService.GetForCurrentSecretaryAsync(studentId, ct);
            if (accessResult.IsFailure)
            {
                return accessResult.ErrorDetails;
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
            qualificationWork.DefenceDate = request.DefenceDate;

            await context.SaveChangesAsync(ct);

            return new UpdateStudentDefenceResponse(student.Id, qualificationWork.DefenceDate);
        }
    }
}
