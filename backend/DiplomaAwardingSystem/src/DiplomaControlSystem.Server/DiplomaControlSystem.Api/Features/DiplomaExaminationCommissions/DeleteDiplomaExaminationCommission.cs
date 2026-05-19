using Core.Api.Extensions;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using DiplomaControlSystem.Api.Infrastructure.Access;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DiplomaControlSystem.Api.Features.DiplomaExaminationCommissions;

public static class DeleteDiplomaExaminationCommission
{
    internal static class Endpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("/diploma-examination-commissions/{commissionId:int}", Handle)
                .WithSummary("Deletes a diploma examination commission")
                .Produces(StatusCodes.Status204NoContent)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithTags("Diploma Examination Commissions");
        }

        private static async Task<Results<NoContent, ProblemHttpResult>> Handle(
            [FromRoute] int commissionId,
            [FromQuery] string secretaryEmail,
            [FromServices] Handler handler,
            CancellationToken ct)
        {
            var result = await handler.HandleAsync(commissionId, secretaryEmail, ct);

            if (result.IsFailure)
            {
                return result.ToProblemDetails();
            }

            return TypedResults.NoContent();
        }
    }

    private sealed class Handler(
        DbDocGenContext context,
        SecretaryAccessService secretaryAccessService) : IScopedService
    {
        public async Task<Result> HandleAsync(
            int commissionId,
            string secretaryEmail,
            CancellationToken ct)
        {
            var secretaryResult = await secretaryAccessService.GetActiveSecretaryAsync(secretaryEmail, ct);
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

            await using var transaction = await context.Database.BeginTransactionAsync(ct);

            await context.Groups
                .Where(group => group.DiplomaExaminationCommissionId == commission.Id)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        group => group.DiplomaExaminationCommissionId,
                        (int?)null),
                    ct);

            await context.Archives
                .Where(archive => archive.DiplomaExaminationCommissionId == commission.Id)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        archive => archive.DiplomaExaminationCommissionId,
                        (int?)null),
                    ct);

            context.DiplomaExaminationCommissions.Remove(commission);
            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return Result.Success();
        }
    }
}
