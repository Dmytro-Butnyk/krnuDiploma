using Core.Api.Extensions;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.Enums;
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

public static class UpdateDefenceResults
{
    public sealed record Request(
        string SecretaryEmail,
        float PlagiarismPercent,
        float UniquePercent,
        int SupervisorScore,
        int ReviewerScore,
        int CommissionScore,
        string EctsGrade,
        string NationalGrade,
        bool HasDiplomaWithHonors);

    public sealed record Response(
        int StudentId,
        float PlagiarismPercent,
        float UniquePercent,
        int SupervisorScore,
        int ReviewerScore,
        int CommissionScore,
        string EctsGrade,
        string NationalGrade,
        bool HasDiplomaWithHonors);

    internal sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.SecretaryEmail)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(320);

            RuleFor(x => x.PlagiarismPercent)
                .InclusiveBetween(0, 100);

            RuleFor(x => x.UniquePercent)
                .InclusiveBetween(0, 100);

            RuleFor(x => x.SupervisorScore)
                .InclusiveBetween(0, 100);

            RuleFor(x => x.ReviewerScore)
                .InclusiveBetween(0, 100);

            RuleFor(x => x.CommissionScore)
                .InclusiveBetween(0, 100);

            RuleFor(x => x.EctsGrade)
                .NotEmpty()
                .Must(BeValidEctsGrade)
                .WithMessage("ECTS grade is invalid.");

            RuleFor(x => x.NationalGrade)
                .NotEmpty()
                .Must(BeValidNationalGrade)
                .WithMessage("National grade is invalid.");
        }

        private static bool BeValidEctsGrade(string value)
        {
            return Enum.TryParse<EctsGrade>(value, ignoreCase: true, out _);
        }

        private static bool BeValidNationalGrade(string value)
        {
            return Enum.TryParse<NationalGrade>(value, ignoreCase: true, out _);
        }
    }

    internal static class Endpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPatch("/students/{studentId:int}/defence-results", Handle)
                .WithSummary("Updates student defence result fields")
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
                .Include(s => s.QualificationWork)
                .FirstOrDefaultAsync(s => s.Id == studentId, ct);

            if (student is null)
            {
                return ErrorDetails.NotFound(
                    "Student.NotFound",
                    "Student was not found.");
            }

            var qualificationWork = StudentDiplomaDataInitializer.EnsureQualificationWork(student);
            qualificationWork.PlagiarismPercent = request.PlagiarismPercent;
            qualificationWork.UniquePercent = request.UniquePercent;
            qualificationWork.SupervisorScore = request.SupervisorScore;
            qualificationWork.ReviewerScore = request.ReviewerScore;
            qualificationWork.CommissionScore = request.CommissionScore;
            qualificationWork.EctsGrade = Enum.Parse<EctsGrade>(request.EctsGrade, ignoreCase: true);
            qualificationWork.NationalGrade = Enum.Parse<NationalGrade>(request.NationalGrade, ignoreCase: true);
            qualificationWork.HasDiplomaWithHonors = request.HasDiplomaWithHonors;

            await context.SaveChangesAsync(ct);

            return new Response(
                student.Id,
                qualificationWork.PlagiarismPercent,
                qualificationWork.UniquePercent,
                qualificationWork.SupervisorScore,
                qualificationWork.ReviewerScore,
                qualificationWork.CommissionScore,
                qualificationWork.EctsGrade.ToString(),
                qualificationWork.NationalGrade.ToString(),
                qualificationWork.HasDiplomaWithHonors);
        }
    }
}
