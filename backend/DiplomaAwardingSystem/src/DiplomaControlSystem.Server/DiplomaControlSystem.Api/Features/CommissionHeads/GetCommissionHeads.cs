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
    public sealed class GetCommissionHeadsRequest
    {
        public string SecretaryEmail { get; init; } = string.Empty;
    }

    internal sealed class Validator : AbstractValidator<GetCommissionHeadsRequest>
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
            app.MapGet("/commission-heads", Handle)
                .WithSummary("Gets active commission heads")
                .Produces<IReadOnlyCollection<CommissionHeadDto>>(StatusCodes.Status200OK)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithTags("Commission Heads");
        }

        private static async Task<Results<Ok<IReadOnlyCollection<CommissionHeadDto>>, ProblemHttpResult, ValidationProblem>> Handle(
            [AsParameters] GetCommissionHeadsRequest request,
            [FromServices] IValidator<GetCommissionHeadsRequest> validator,
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
        public async Task<Result<IReadOnlyCollection<CommissionHeadDto>>> HandleAsync(
            GetCommissionHeadsRequest request,
            CancellationToken ct)
        {
            var secretaryResult = await secretaryAccessService.GetActiveSecretaryAsync(request.SecretaryEmail, ct);
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
                    head.Position,
                    head.Company,
                    head.Specialty,
                    head.IsDeleted))
                .ToListAsync(ct);
        }
    }
}
