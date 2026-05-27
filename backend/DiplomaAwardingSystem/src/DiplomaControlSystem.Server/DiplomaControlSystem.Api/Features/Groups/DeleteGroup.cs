using Core.Api.Extensions;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using DiplomaControlSystem.Api.Infrastructure.Access;
using DiplomaControlSystem.Api.Infrastructure.DiplomaExaminationCommissions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DiplomaControlSystem.Api.Features.Groups;

public static class DeleteGroup
{
    internal static class Endpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("/groups/{groupId:int}", Handle)
                .WithSummary("Deletes a group with students")
                .Produces(StatusCodes.Status204NoContent)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithTags("Groups");
        }

        private static async Task<Results<NoContent, ProblemHttpResult>> Handle(
            [FromRoute] int groupId,
            [FromQuery] string secretaryEmail,
            [FromServices] Handler handler,
            CancellationToken ct)
        {
            var result = await handler.HandleAsync(groupId, secretaryEmail, ct);

            if (result.IsFailure)
            {
                return result.ToProblemDetails();
            }

            return TypedResults.NoContent();
        }
    }

    private sealed class Handler(
        DbDocGenContext context,
        SecretaryAccessService secretaryAccessService,
        DiplomaExaminationCommissionCleanupService commissionCleanupService) : IScopedService
    {
        public async Task<Result> HandleAsync(
            int groupId,
            string secretaryEmail,
            CancellationToken ct)
        {
            var secretaryResult = await secretaryAccessService.GetActiveSecretaryAsync(secretaryEmail, ct);
            if (secretaryResult.IsFailure)
            {
                return secretaryResult.ErrorDetails;
            }

            var secretary = secretaryResult.Value!;
            var group = await context.Groups.FirstOrDefaultAsync(g => g.Id == groupId, ct);

            if (group is null)
            {
                return ErrorDetails.NotFound(
                    "Group.NotFound",
                    "Group was not found.");
            }

            if (group.SpecialtyId != secretary.SpecialtyId)
            {
                return ErrorDetails.Forbidden(
                    "Group.Forbidden",
                    "Group does not belong to secretary specialty.");
            }

            var commissionId = group.DiplomaExaminationCommissionId;

            await using var transaction = await context.Database.BeginTransactionAsync(ct);

            context.Groups.Remove(group);
            await context.SaveChangesAsync(ct);

            await commissionCleanupService.RemoveEmptyCommissionsAsync([commissionId], ct);
            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return Result.Success();
        }
    }
}
