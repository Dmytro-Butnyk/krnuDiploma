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

public static class UpdateQualificationWorkCharacteristics
{
    public sealed record Request(
        string SecretaryEmail,
        bool IsResearchBased,
        bool HasRealProjects,
        bool IsEcoFriendly,
        bool IsEnterpriseOrdered,
        bool IsComplexInteruniversity,
        bool IsComplexInterdepartmental,
        bool IsComplexDepartmental,
        bool IsComplexProjectParticipant,
        bool IsRecommendedForMaster,
        bool IsRecommendedForImplementation,
        bool IsDefendedAtEnterprise);

    public sealed record Response(
        int StudentId,
        bool IsResearchBased,
        bool HasRealProjects,
        bool IsEcoFriendly,
        bool IsEnterpriseOrdered,
        bool IsComplexInteruniversity,
        bool IsComplexInterdepartmental,
        bool IsComplexDepartmental,
        bool IsComplexProjectParticipant,
        bool IsRecommendedForMaster,
        bool IsRecommendedForImplementation,
        bool IsDefendedAtEnterprise);

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
            app.MapPatch("/students/{studentId:int}/qualification-work-characteristics", Handle)
                .WithSummary("Updates student qualification work characteristics")
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
                .ThenInclude(qw => qw!.QualificationWorkCharacteristics)
                .FirstOrDefaultAsync(s => s.Id == studentId, ct);

            if (student is null)
            {
                return ErrorDetails.NotFound(
                    "Student.NotFound",
                    "Student was not found.");
            }

            var qualificationWork = StudentDiplomaDataInitializer.EnsureQualificationWork(student);
            var characteristics = StudentDiplomaDataInitializer.EnsureCharacteristics(qualificationWork);
            characteristics.IsResearchBased = request.IsResearchBased;
            characteristics.HasRealProjects = request.HasRealProjects;
            characteristics.IsEcoFriendly = request.IsEcoFriendly;
            characteristics.IsEnterpriseOrdered = request.IsEnterpriseOrdered;
            characteristics.IsComplexInteruniversity = request.IsComplexInteruniversity;
            characteristics.IsComplexInterdepartmental = request.IsComplexInterdepartmental;
            characteristics.IsComplexDepartmental = request.IsComplexDepartmental;
            characteristics.IsComplexProjectParticipant = request.IsComplexProjectParticipant;
            characteristics.IsRecommendedForMaster = request.IsRecommendedForMaster;
            characteristics.IsRecommendedForImplementation = request.IsRecommendedForImplementation;
            characteristics.IsDefendedAtEnterprise = request.IsDefendedAtEnterprise;

            await context.SaveChangesAsync(ct);

            return new Response(
                student.Id,
                characteristics.IsResearchBased,
                characteristics.HasRealProjects,
                characteristics.IsEcoFriendly,
                characteristics.IsEnterpriseOrdered,
                characteristics.IsComplexInteruniversity,
                characteristics.IsComplexInterdepartmental,
                characteristics.IsComplexDepartmental,
                characteristics.IsComplexProjectParticipant,
                characteristics.IsRecommendedForMaster,
                characteristics.IsRecommendedForImplementation,
                characteristics.IsDefendedAtEnterprise);
        }
    }
}
