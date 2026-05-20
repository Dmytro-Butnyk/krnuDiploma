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

public static class GetDiplomaExaminationCommissions
{
    public sealed class GetDiplomaExaminationCommissionsRequest
    {
        public string SecretaryEmail { get; init; } = string.Empty;
        public string EducationLevel { get; init; } = string.Empty;
        public string DefenseYear { get; init; } = string.Empty;
    }

    internal sealed class Validator : AbstractValidator<GetDiplomaExaminationCommissionsRequest>
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
                .NotEmpty()
                .Must(year => GroupYearRules.TryNormalizeDefenseYear(year, out _))
                .WithMessage("Defense year must be a 4-digit year like 2026.");
        }
    }

    internal static class Endpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/diploma-examination-commissions", Handle)
                .WithSummary("Gets diploma examination commissions by defense year and education level")
                .Produces<IReadOnlyCollection<DiplomaExaminationCommissionResponse>>(StatusCodes.Status200OK)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithTags("Diploma Examination Commissions");
        }

        private static async Task<Results<Ok<IReadOnlyCollection<DiplomaExaminationCommissionResponse>>, ProblemHttpResult, ValidationProblem>> Handle(
            [AsParameters] GetDiplomaExaminationCommissionsRequest request,
            [FromServices] IValidator<GetDiplomaExaminationCommissionsRequest> validator,
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
        public async Task<Result<IReadOnlyCollection<DiplomaExaminationCommissionResponse>>> HandleAsync(
            GetDiplomaExaminationCommissionsRequest request,
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

            var commissions = await context.DiplomaExaminationCommissions
                .AsNoTracking()
                .Include(dec => dec.Groups)
                .Include(dec => dec.HeadTeacher)
                .Include(dec => dec.FirstMemberTeacher)
                .Include(dec => dec.SecondMemberTeacher)
                .Include(dec => dec.ThirdMemberTeacher)
                .Include(dec => dec.Secretary)
                .Where(dec => dec.EducationLevel == educationLevel)
                .Where(dec => dec.Groups.Any(group =>
                    group.SpecialtyId == secretary.SpecialtyId
                    && group.Year == defenseYear))
                .OrderBy(dec => dec.OrderNumber)
                .ToListAsync(ct);

            return commissions
                .Select(dec => DiplomaExaminationCommissionUpsertSupport.Map(dec, defenseYear))
                .ToList();
        }
    }
}
