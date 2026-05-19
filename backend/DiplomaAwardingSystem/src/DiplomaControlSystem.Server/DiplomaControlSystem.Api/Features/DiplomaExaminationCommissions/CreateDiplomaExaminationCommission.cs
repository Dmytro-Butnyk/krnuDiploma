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
using DomainCommission = Core.Domain.Entities.TeacherStaff.DiplomaExaminationCommission;

namespace DiplomaControlSystem.Api.Features.DiplomaExaminationCommissions;

public static class CreateDiplomaExaminationCommission
{
    internal static class Endpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/diploma-examination-commissions", Handle)
                .WithSummary("Creates a diploma examination commission")
                .Produces<CommissionDto>(StatusCodes.Status201Created)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status409Conflict)
                .WithTags("Diploma Examination Commissions");
        }

        private static async Task<Results<Created<CommissionDto>, ProblemHttpResult, ValidationProblem>> Handle(
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
        public async Task<Result<CommissionDto>> HandleAsync(
            UpsertRequest request,
            CancellationToken ct)
        {
            var secretaryResult = await secretaryAccessService.GetActiveSecretaryAsync(request.SecretaryEmail, ct);
            if (secretaryResult.IsFailure)
            {
                return secretaryResult.ErrorDetails;
            }

            var secretary = secretaryResult.Value!;
            var validationResult = await DiplomaExaminationCommissionUpsertSupport.ValidateAsync(
                context,
                request,
                secretary,
                commissionId: null,
                ct);

            if (validationResult.IsFailure)
            {
                return validationResult.ErrorDetails;
            }

            var validated = validationResult.Value!;
            await using var transaction = await context.Database.BeginTransactionAsync(ct);

            var commission = new DomainCommission(
                request.OrderNumber,
                validated.EducationLevel,
                request.StartDate,
                request.EndDate,
                validated.HeadTeacherId,
                validated.HeadPersonaName,
                validated.HeadPersonaPosition,
                request.FirstMemberTeacherId,
                request.SecondMemberTeacherId,
                request.ThirdMemberTeacherId,
                secretary.SecretaryId);

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
                validated.DefenseYear,
                ct);
        }
    }
}
