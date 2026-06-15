using Core.Api.Extensions;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using DiplomaControlSystem.Api.Contracts.Common;
using DiplomaControlSystem.Api.Infrastructure.Access;
using DiplomaControlSystem.Api.Infrastructure.CommissionHeads;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static DiplomaControlSystem.Api.Contracts.CommissionHeads.CommissionHeadContracts;

namespace DiplomaControlSystem.Api.Features.CommissionHeads;

public static class UpdateCommissionHead
{
    public sealed record UpdateCommissionHeadRequest(
        string FullName,
        PersonNameFormsDto? NameForms,
        string Position,
        string Company,
        string Specialty) : ICommissionHeadRequest;

    internal sealed class Validator : AbstractValidator<UpdateCommissionHeadRequest>
    {
        public Validator()
        {
            Include(new CommissionHeadRequestValidator<UpdateCommissionHeadRequest>());
        }
    }

    internal static class Endpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("/commission-heads/{commissionHeadId:int}", Handle)
                .WithSummary("Updates a commission head")
                .Produces<CommissionHeadDto>(StatusCodes.Status200OK)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status409Conflict)
                .WithTags("Commission Heads");
        }

        private static async Task<Results<Ok<CommissionHeadDto>, ProblemHttpResult, ValidationProblem>> Handle(
            [FromRoute] int commissionHeadId,
            [FromBody] UpdateCommissionHeadRequest request,
            [FromServices] IValidator<UpdateCommissionHeadRequest> validator,
            [FromServices] Handler handler,
            CancellationToken ct)
        {
            ValidationResult validationResult = await validator.ValidateAsync(request, ct);

            if (!validationResult.IsValid)
            {
                return TypedResults.ValidationProblem(validationResult.ToDictionary());
            }

            var result = await handler.HandleAsync(commissionHeadId, request, ct);

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
        public async Task<Result<CommissionHeadDto>> HandleAsync(
            int commissionHeadId,
            UpdateCommissionHeadRequest request,
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

            if (commissionHead.IsDeleted)
            {
                return ErrorDetails.Conflict(
                    "CommissionHead.Deleted",
                    "Deleted commission head cannot be updated.");
            }

            var normalized = CommissionHeadRequestSupport.Normalize(request);

            var duplicateExists = await CommissionHeadRequestSupport.ActiveDuplicateExistsAsync(
                context,
                normalized,
                exceptId: commissionHead.Id,
                ct);

            if (duplicateExists)
            {
                return ErrorDetails.Conflict(
                    "CommissionHead.AlreadyExists",
                    "Active commission head with the same data already exists.");
            }

            commissionHead.FullName = normalized.FullName;
            commissionHead.NameForms = request.NameForms?.ToDomain(normalized.FullName)
                ?? Core.Domain.Entities.PersonNameForms.FromDefault(normalized.FullName);
            commissionHead.Position = normalized.Position;
            commissionHead.Company = normalized.Company;
            commissionHead.Specialty = normalized.Specialty;

            await context.SaveChangesAsync(ct);

            return CommissionHeadRequestSupport.Map(commissionHead);
        }
    }
}
