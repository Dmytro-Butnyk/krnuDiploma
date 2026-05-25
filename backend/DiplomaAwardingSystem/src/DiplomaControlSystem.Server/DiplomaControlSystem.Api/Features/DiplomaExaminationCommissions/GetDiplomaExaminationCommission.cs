using Core.Api.Extensions;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using DiplomaControlSystem.Api.Contracts.DiplomaExaminationCommissions;
using DiplomaControlSystem.Api.Infrastructure.Access;
using DiplomaControlSystem.Api.Infrastructure.DiplomaExaminationCommissions;
using DiplomaControlSystem.Api.Infrastructure.Groups;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static DiplomaControlSystem.Api.Contracts.DiplomaExaminationCommissions.DiplomaExaminationCommissionContracts;

namespace DiplomaControlSystem.Api.Features.DiplomaExaminationCommissions;

public static class GetDiplomaExaminationCommission
{
    public sealed class GetDiplomaExaminationCommissionRequest
    {
        public string SecretaryEmail { get; init; } = string.Empty;
        public string EducationLevel { get; init; } = string.Empty;
        public string DefenseYear { get; init; } = string.Empty;
    }

    internal sealed class Validator : AbstractValidator<GetDiplomaExaminationCommissionRequest>
    {
        public Validator()
        {
            RuleFor(x => x.SecretaryEmail)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(320);

            RuleFor(x => x.EducationLevel)
                .NotEmpty()
                .Must(level => DiplomaExaminationCommissionRules.TryParseEducationLevel(level, out _))
                .WithMessage("Education level must be Bachelor or Master.");

            RuleFor(x => x.DefenseYear)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .Must(year => GroupYearRules.TryNormalizeDefenseYear(year, out _))
                .WithMessage("Defense year must be a 4-digit year like 2026.")
                .Must(GroupYearRules.IsAllowedDefenseYear)
                .WithMessage(_ => GroupYearRules.GetAllowedDefenseYearRangeMessage());
        }
    }

    internal static class Endpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/diploma-examination-commissions", Handle)
                .WithSummary("Gets diploma examination commission by defense year, specialty, and education level")
                .Produces<DiplomaExaminationCommissionResponse>(StatusCodes.Status200OK)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithTags("Diploma Examination Commissions");
        }

        private static async Task<Results<Ok<DiplomaExaminationCommissionResponse>, ProblemHttpResult, ValidationProblem>> Handle(
            [AsParameters] GetDiplomaExaminationCommissionRequest request,
            [FromServices] IValidator<GetDiplomaExaminationCommissionRequest> validator,
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

            return TypedResults.Ok(result.Value!);
        }
    }

    private sealed class Handler(
        DbDocGenContext context,
        SecretaryAccessService secretaryAccessService) : IScopedService
    {
        public async Task<Result<DiplomaExaminationCommissionResponse>> HandleAsync(
            GetDiplomaExaminationCommissionRequest request,
            CancellationToken ct)
        {
            var secretaryResult = await secretaryAccessService.GetActiveSecretaryAsync(request.SecretaryEmail, ct);
            if (secretaryResult.IsFailure)
            {
                return secretaryResult.ErrorDetails;
            }

            var secretary = secretaryResult.Value!;
            _ = DiplomaExaminationCommissionRules.TryParseEducationLevel(request.EducationLevel, out var educationLevel);
            _ = GroupYearRules.TryNormalizeDefenseYear(request.DefenseYear, out var defenseYear);

            var commission = await context.DiplomaExaminationCommissions
                .AsNoTracking()
                .Include(dec => dec.Groups)
                .Include(dec => dec.CommissionHead)
                .Include(dec => dec.FirstMemberTeacher)
                .Include(dec => dec.SecondMemberTeacher)
                .Include(dec => dec.ThirdMemberTeacher)
                .Include(dec => dec.Secretary)
                .Where(dec => dec.DefenseYear == defenseYear)
                .Where(dec => dec.SpecialtyId == secretary.SpecialtyId)
                .Where(dec => dec.EducationLevel == educationLevel)
                .FirstOrDefaultAsync(ct);

            if (commission is null)
            {
                return ErrorDetails.NotFound(
                    "DiplomaExaminationCommission.NotFound",
                    "Diploma examination commission was not found.");
            }

            return DiplomaExaminationCommissionUpsertSupport.Map(commission);
        }
    }
}
