using Core.Api.Extensions;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using DiplomaControlSystem.Api.Contracts.DiplomaExaminationCommissions;
using DiplomaControlSystem.Api.Infrastructure.Access;
using DiplomaControlSystem.Api.Infrastructure.DiplomaExaminationCommissions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static DiplomaControlSystem.Api.Contracts.DiplomaExaminationCommissions.DiplomaExaminationCommissionContracts;
using DomainCommission = Core.Domain.Entities.TeacherStaff.DiplomaExaminationCommission;

namespace DiplomaControlSystem.Api.Features.DiplomaExaminationCommissions;

public static class CreateDiplomaExaminationCommission
{
    public sealed class CreateDiplomaExaminationCommissionRequest : DiplomaExaminationCommissionCreateRequest;

    internal sealed class Validator : DiplomaExaminationCommissionCreateValidator<CreateDiplomaExaminationCommissionRequest>;

    internal static class Endpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/diploma-examination-commissions", Handle)
                .WithSummary("Creates a diploma examination commission")
                .Produces<DiplomaExaminationCommissionResponse>(StatusCodes.Status201Created)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status409Conflict)
                .WithTags("Diploma Examination Commissions");
        }

        private static async Task<Results<Created<DiplomaExaminationCommissionResponse>, ProblemHttpResult, ValidationProblem>> Handle(
            [FromBody] CreateDiplomaExaminationCommissionRequest request,
            [FromServices] IValidator<CreateDiplomaExaminationCommissionRequest> validator,
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

            return TypedResults.Created(
                $"/api/diploma-examination-commissions/{result.Value!.Id}",
                result.Value);
        }
    }

    private sealed class Handler(
        DbDocGenContext context,
        SecretaryAccessService secretaryAccessService) : IScopedService
    {
        public async Task<Result<DiplomaExaminationCommissionResponse>> HandleAsync(
            CreateDiplomaExaminationCommissionRequest request,
            CancellationToken ct)
        {
            var secretaryResult = await secretaryAccessService.GetActiveSecretaryAsync(request.SecretaryEmail, ct);
            if (secretaryResult.IsFailure)
            {
                return secretaryResult.ErrorDetails;
            }

            var secretary = secretaryResult.Value!;
            var validationResult = await DiplomaExaminationCommissionUpsertSupport.ValidateCreateAsync(
                context,
                request,
                secretary,
                ct);

            if (validationResult.IsFailure)
            {
                return validationResult.ErrorDetails;
            }

            var validated = validationResult.Value!;
            await using var transaction = await context.Database.BeginTransactionAsync(ct);

            var commission = new DomainCommission(
                validated.OrderNumber,
                validated.EducationLevel,
                validated.DefenseYear,
                request.StartDate,
                request.EndDate,
                validated.SpecialtyId,
                validated.CommissionHeadId,
                request.FirstMemberTeacherId,
                request.SecondMemberTeacherId,
                request.ThirdMemberTeacherId,
                validated.SecretaryId);

            foreach (var group in validated.Groups)
            {
                commission.Groups.Add(group);
            }

            await context.DiplomaExaminationCommissions.AddAsync(commission, ct);
            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return await DiplomaExaminationCommissionUpsertSupport.GetDtoAsync(
                context,
                commission.Id,
                ct);
        }
    }
}
