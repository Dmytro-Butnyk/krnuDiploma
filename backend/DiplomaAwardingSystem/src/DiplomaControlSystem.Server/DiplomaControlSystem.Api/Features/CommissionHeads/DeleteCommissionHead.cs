using Core.Api.Extensions;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using DiplomaControlSystem.Api.Infrastructure.Access;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DiplomaControlSystem.Api.Features.CommissionHeads;

public static class DeleteCommissionHead
{
    internal static class Endpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("/commission-heads/{commissionHeadId:int}", Handle)
                .WithSummary("Soft deletes a commission head")
                .Produces(StatusCodes.Status204NoContent)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithTags("Commission Heads");
        }

        private static async Task<Results<NoContent, ProblemHttpResult>> Handle(
            [FromRoute] int commissionHeadId,
            [FromServices] Handler handler,
            CancellationToken ct)
        {
            var result = await handler.HandleAsync(commissionHeadId, ct);

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
            int commissionHeadId,
            CancellationToken ct)
        {
            var secretaryResult = await secretaryAccessService.GetCurrentSecretaryAsync(ct);
            if (secretaryResult.IsFailure)
            {
                return secretaryResult.ErrorDetails;
            }

            var commissionHead = await context.CommissionHeads
                .FirstOrDefaultAsync(head => head.Id == commissionHeadId, ct);

            if (commissionHead is null)
            {
                return ErrorDetails.NotFound(
                    "CommissionHead.NotFound",
                    "Commission head was not found.");
            }

            commissionHead.IsDeleted = true;
            await context.SaveChangesAsync(ct);

            return Result.Success();
        }
    }
}
