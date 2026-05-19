using Core.Api.Extensions;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using DiplomaControlSystem.Api.Infrastructure.Access;
using DiplomaControlSystem.Api.Infrastructure.Groups;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static DiplomaControlSystem.Api.Features.DiplomaExaminationCommissions.DiplomaExaminationCommissionContracts;

namespace DiplomaControlSystem.Api.Features.DiplomaExaminationCommissions;

public static class GetDiplomaExaminationCommissionOptions
{
    public sealed class Request
    {
        public string SecretaryEmail { get; init; } = string.Empty;
        public string EducationLevel { get; init; } = string.Empty;
        public string DefenseYear { get; init; } = string.Empty;
        public int? CommissionId { get; init; }
    }

    public sealed record Response(
        IReadOnlyCollection<GroupDto> Groups,
        IReadOnlyCollection<TeacherDto> Teachers,
        SecretaryDto Secretary);

    internal sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.SecretaryEmail)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(320);

            RuleFor(x => x.EducationLevel)
                .NotEmpty()
                .Must(level => Rules.TryParseEducationLevel(level, out _))
                .WithMessage("Education level must be Bachelor or Master.");

            RuleFor(x => x.DefenseYear)
                .NotEmpty()
                .Must(year => GroupYearRules.TryNormalizeDefenseYear(year, out _))
                .WithMessage("Defense year must be a 4-digit year like 2026.");

            RuleFor(x => x.CommissionId)
                .GreaterThan(0)
                .When(x => x.CommissionId is not null);
        }
    }

    internal static class Endpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/diploma-examination-commissions/options", Handle)
                .WithSummary("Gets available groups and teachers for diploma examination commission form")
                .Produces<Response>(StatusCodes.Status200OK)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithTags("Diploma Examination Commissions");
        }

        private static async Task<Results<Ok<Response>, ProblemHttpResult, ValidationProblem>> Handle(
            [AsParameters] Request request,
            [FromServices] IValidator<Request> validator,
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
        public async Task<Result<Response>> HandleAsync(Request request, CancellationToken ct)
        {
            var secretaryResult = await secretaryAccessService.GetActiveSecretaryAsync(request.SecretaryEmail, ct);
            if (secretaryResult.IsFailure)
            {
                return secretaryResult.ErrorDetails;
            }

            var secretary = secretaryResult.Value!;
            _ = Rules.TryParseEducationLevel(request.EducationLevel, out var educationLevel);
            _ = GroupYearRules.TryNormalizeDefenseYear(request.DefenseYear, out var defenseYear);

            if (request.CommissionId is not null)
            {
                var commissionBelongsToSecretary = await context.DiplomaExaminationCommissions
                    .AsNoTracking()
                    .AnyAsync(
                        dec => dec.Id == request.CommissionId
                               && (dec.SecretaryId == secretary.SecretaryId
                                   || dec.Groups.Any(group => group.SpecialtyId == secretary.SpecialtyId)),
                        ct);

                if (!commissionBelongsToSecretary)
                {
                    return ErrorDetails.NotFound(
                        "DiplomaExaminationCommission.NotFound",
                        "Diploma examination commission was not found.");
                }
            }

            var groups = await context.Groups
                .AsNoTracking()
                .Where(group => group.SpecialtyId == secretary.SpecialtyId)
                .Where(group => group.EducationLevel == educationLevel)
                .Where(group => group.Year == defenseYear)
                .Where(group => group.DiplomaExaminationCommissionId == null
                                || group.DiplomaExaminationCommissionId == request.CommissionId)
                .OrderBy(group => group.Name)
                .Select(group => new GroupDto(group.Id, group.Name))
                .ToListAsync(ct);

            var teachers = await context.Teachers
                .AsNoTracking()
                .Where(teacher => teacher.SpecialtyId == secretary.SpecialtyId)
                .OrderBy(teacher => teacher.ShortName)
                .Select(teacher => new TeacherDto(
                    teacher.Id,
                    teacher.FullName,
                    teacher.ShortName,
                    teacher.Position))
                .ToListAsync(ct);

            return new Response(
                groups,
                teachers,
                new SecretaryDto(secretary.SecretaryId, secretary.FullName));
        }
    }
}
