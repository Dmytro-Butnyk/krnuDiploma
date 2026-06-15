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
using static DiplomaControlSystem.Api.Contracts.CommissionHeads.CommissionHeadContracts;

namespace DiplomaControlSystem.Api.Features.CommissionHeads;

public static class GetCommissionHeads
{
    internal static class Endpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/commission-heads", Handle)
                .WithSummary("Gets active commission heads")
                .Produces<IReadOnlyCollection<CommissionHeadDto>>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithTags("Commission Heads");
        }

        private static async Task<Results<Ok<IReadOnlyCollection<CommissionHeadDto>>, ProblemHttpResult>> Handle(
            [FromServices] Handler handler,
            CancellationToken ct)
        {
            var result = await handler.HandleAsync(ct);

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
        public async Task<Result<IReadOnlyCollection<CommissionHeadDto>>> HandleAsync(
            CancellationToken ct)
        {
            var secretaryResult = await secretaryAccessService.GetCurrentSecretaryAsync(ct);
            if (secretaryResult.IsFailure)
            {
                return secretaryResult.ErrorDetails;
            }

            return await context.CommissionHeads
                .AsNoTracking()
                .Where(head => !head.IsDeleted)
                .OrderBy(head => head.FullName)
                .Select(head => new CommissionHeadDto(
                    head.Id,
                    head.FullName,
                    Contracts.Common.PersonNameFormsDto.From(head.NameForms),
                    head.Position,
                    head.Company,
                    head.Specialty,
                    head.IsDeleted))
                .ToListAsync(ct);
        }
    }
}
