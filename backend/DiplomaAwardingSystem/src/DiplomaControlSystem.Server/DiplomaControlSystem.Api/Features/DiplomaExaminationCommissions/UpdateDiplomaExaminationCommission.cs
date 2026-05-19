using Core.Api.Extensions;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using DiplomaControlSystem.Api.Infrastructure.Access;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static DiplomaControlSystem.Api.Features.DiplomaExaminationCommissions.DiplomaExaminationCommissionContracts;

namespace DiplomaControlSystem.Api.Features.DiplomaExaminationCommissions;

public static class UpdateDiplomaExaminationCommission
{
    internal static class Endpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("/diploma-examination-commissions/{commissionId:int}", Handle)
                .WithSummary("Updates a diploma examination commission")
                .Produces<CommissionDto>(StatusCodes.Status200OK)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status409Conflict)
                .WithTags("Diploma Examination Commissions");
        }

        private static async Task<Results<Ok<CommissionDto>, ProblemHttpResult, ValidationProblem>> Handle(
            [FromRoute] int commissionId,
            [FromBody] UpsertRequest request,
            [FromServices] IValidator<UpsertRequest> validator,
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
        public async Task<Result<CommissionDto>> HandleAsync(
            int commissionId,
            UpsertRequest request,
            CancellationToken ct)
        {
            var secretaryResult = await secretaryAccessService.GetActiveSecretaryAsync(request.SecretaryEmail, ct);
            if (secretaryResult.IsFailure)
            {
                return secretaryResult.ErrorDetails;
            }

            var secretary = secretaryResult.Value!;
            var commission = await context.DiplomaExaminationCommissions
                .Include(dec => dec.Groups)
                .FirstOrDefaultAsync(dec => dec.Id == commissionId, ct);

            if (commission is null)
            {
                return ErrorDetails.NotFound(
                    "DiplomaExaminationCommission.NotFound",
                    "Diploma examination commission was not found.");
            }

            if (commission.SecretaryId != secretary.SecretaryId
                && !commission.Groups.Any(group => group.SpecialtyId == secretary.SpecialtyId))
            {
                return ErrorDetails.Forbidden(
                    "DiplomaExaminationCommission.Forbidden",
                    "Diploma examination commission does not belong to secretary specialty.");
            }

            var validationResult = await DiplomaExaminationCommissionUpsertSupport.ValidateAsync(
                context,
                request,
                secretary,
                commissionId,
                ct);

            if (validationResult.IsFailure)
            {
                return validationResult.ErrorDetails;
            }

            var validated = validationResult.Value!;
            var selectedGroupIds = validated.Groups.Select(group => group.Id).ToHashSet();

            await using var transaction = await context.Database.BeginTransactionAsync(ct);

            foreach (var group in commission.Groups.Where(group => !selectedGroupIds.Contains(group.Id)).ToList())
            {
                group.DiplomaExaminationCommissionId = null;
            }

            foreach (var group in validated.Groups)
            {
                group.DiplomaExaminationCommissionId = commission.Id;
            }

            commission.OrderNumber = request.OrderNumber;
            commission.EducationLevel = validated.EducationLevel;
            commission.StartDate = request.StartDate;
            commission.EndDate = request.EndDate;
            commission.HeadTeacherId = validated.HeadTeacherId;
            commission.HeadPersonaName = validated.HeadPersonaName;
            commission.HeadPersonaPosition = validated.HeadPersonaPosition;
            commission.FirstMemberTeacherId = request.FirstMemberTeacherId;
            commission.SecondMemberTeacherId = request.SecondMemberTeacherId;
            commission.ThirdMemberTeacherId = request.ThirdMemberTeacherId;
            commission.SecretaryId = secretary.SecretaryId;

            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return await DiplomaExaminationCommissionUpsertSupport.GetDtoAsync(
                context,
                commission.Id,
                validated.DefenseYear,
                ct);
        }
    }
}
