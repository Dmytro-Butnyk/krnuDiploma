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

namespace DiplomaControlSystem.Api.Features.DiplomaExaminationCommissions;

public static class UpdateDiplomaExaminationCommission
{
    public sealed class UpdateDiplomaExaminationCommissionRequest : DiplomaExaminationCommissionUpdateRequest;

    internal sealed class Validator : DiplomaExaminationCommissionUpdateValidator<UpdateDiplomaExaminationCommissionRequest>;

    internal static class Endpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("/diploma-examination-commissions/{commissionId:int}", Handle)
                .WithSummary("Updates a diploma examination commission")
                .Produces<DiplomaExaminationCommissionResponse>(StatusCodes.Status200OK)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status409Conflict)
                .WithTags("Diploma Examination Commissions");
        }

        private static async Task<Results<Ok<DiplomaExaminationCommissionResponse>, ProblemHttpResult, ValidationProblem>> Handle(
            [FromRoute] int commissionId,
            [FromBody] UpdateDiplomaExaminationCommissionRequest request,
            [FromServices] IValidator<UpdateDiplomaExaminationCommissionRequest> validator,
            [FromServices] Handler handler,
            CancellationToken ct)
        {
            ValidationResult validationResult = await validator.ValidateAsync(request, ct);

            if (!validationResult.IsValid)
            {
                return TypedResults.ValidationProblem(validationResult.ToDictionary());
            }

            var result = await handler.HandleAsync(commissionId, request, ct);

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
            int commissionId,
            UpdateDiplomaExaminationCommissionRequest request,
            CancellationToken ct)
        {
            var secretaryResult = await secretaryAccessService.GetActiveSecretaryAsync(request.SecretaryEmail, ct);
            if (secretaryResult.IsFailure)
            {
                return secretaryResult.ErrorDetails;
            }

            var secretary = secretaryResult.Value!;
            var commission = await context.DiplomaExaminationCommissions
                .FirstOrDefaultAsync(dec => dec.Id == commissionId, ct);

            if (commission is null)
            {
                return ErrorDetails.NotFound(
                    "DiplomaExaminationCommission.NotFound",
                    "Diploma examination commission was not found.");
            }

            if (commission.SpecialtyId != secretary.SpecialtyId)
            {
                return ErrorDetails.Forbidden(
                    "DiplomaExaminationCommission.Forbidden",
                    "Diploma examination commission does not belong to secretary specialty.");
            }

            var validationResult = await DiplomaExaminationCommissionUpsertSupport.ValidateUpdateAsync(
                context,
                request,
                commission,
                secretary,
                ct);

            if (validationResult.IsFailure)
            {
                return validationResult.ErrorDetails;
            }

            var validated = validationResult.Value!;

            await using var transaction = await context.Database.BeginTransactionAsync(ct);

            commission.OrderNumber = validated.OrderNumber;
            commission.StartDate = request.StartDate;
            commission.EndDate = request.EndDate;
            commission.CommissionHeadId = validated.CommissionHeadId;
            commission.FirstMemberTeacherId = request.FirstMemberTeacherId;
            commission.SecondMemberTeacherId = request.SecondMemberTeacherId;
            commission.ThirdMemberTeacherId = request.ThirdMemberTeacherId;
            commission.SecretaryId = validated.SecretaryId;

            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return await DiplomaExaminationCommissionUpsertSupport.GetDtoAsync(
                context,
                commission.Id,
                ct);
        }
    }
}
