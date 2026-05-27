using Core.Api.Extensions;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using DiplomaControlSystem.Api.Contracts.DiplomaExaminationCommissions;
using DiplomaControlSystem.Api.Infrastructure.Access;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static DiplomaControlSystem.Api.Contracts.CommissionHeads.CommissionHeadContracts;
using static DiplomaControlSystem.Api.Contracts.DiplomaExaminationCommissions.DiplomaExaminationCommissionContracts;

namespace DiplomaControlSystem.Api.Features.DiplomaExaminationCommissions;

public static class GetDiplomaExaminationCommissionOptions
{
    public sealed class GetDiplomaExaminationCommissionOptionsRequest
    {
        public string SecretaryEmail { get; init; } = string.Empty;
    }

    public sealed record GetDiplomaExaminationCommissionOptionsResponse(
        IReadOnlyCollection<TeacherDto> Teachers,
        IReadOnlyCollection<SecretaryDto> Secretaries,
        IReadOnlyCollection<CommissionHeadDto> CommissionHeads);

    internal sealed class Validator : AbstractValidator<GetDiplomaExaminationCommissionOptionsRequest>
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
            app.MapGet("/diploma-examination-commissions/options", Handle)
                .WithSummary("Gets available teachers, secretaries, and heads for diploma examination commission form")
                .Produces<GetDiplomaExaminationCommissionOptionsResponse>(StatusCodes.Status200OK)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithTags("Diploma Examination Commissions");
        }

        private static async Task<Results<Ok<GetDiplomaExaminationCommissionOptionsResponse>, ProblemHttpResult, ValidationProblem>> Handle(
            [AsParameters] GetDiplomaExaminationCommissionOptionsRequest request,
            [FromServices] IValidator<GetDiplomaExaminationCommissionOptionsRequest> validator,
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
        public async Task<Result<GetDiplomaExaminationCommissionOptionsResponse>> HandleAsync(GetDiplomaExaminationCommissionOptionsRequest request, CancellationToken ct)
        {
            var secretaryResult = await secretaryAccessService.GetActiveSecretaryAsync(request.SecretaryEmail, ct);
            if (secretaryResult.IsFailure)
            {
                return secretaryResult.ErrorDetails;
            }

            var secretary = secretaryResult.Value!;

            var teachers = await context.Teachers
                .AsNoTracking()
                .Where(teacher => teacher.SpecialtyId == secretary.SpecialtyId)
                .OrderBy(teacher => teacher.FullName)
                .Select(teacher => new TeacherDto(
                    teacher.Id,
                    teacher.FullName,
                    teacher.TeacherPosition != null ? teacher.TeacherPosition.FullName : string.Empty))
                .ToListAsync(ct);

            var secretaries = await context.Secretaries
                .AsNoTracking()
                .Where(candidate => candidate.SpecialtyId == secretary.SpecialtyId)
                .Where(candidate => candidate.IsActive)
                .OrderBy(candidate => candidate.FullName)
                .Select(candidate => new SecretaryDto(candidate.Id, candidate.FullName))
                .ToListAsync(ct);

            var commissionHeads = await context.CommissionHeads
                .AsNoTracking()
                .Where(head => !head.IsDeleted)
                .OrderBy(head => head.FullName)
                .Select(head => new CommissionHeadDto(
                    head.Id,
                    head.FullName,
                    head.Position,
                    head.Company,
                    head.Specialty,
                    head.IsDeleted))
                .ToListAsync(ct);

            return new GetDiplomaExaminationCommissionOptionsResponse(
                teachers,
                secretaries,
                commissionHeads);
        }
    }
}
