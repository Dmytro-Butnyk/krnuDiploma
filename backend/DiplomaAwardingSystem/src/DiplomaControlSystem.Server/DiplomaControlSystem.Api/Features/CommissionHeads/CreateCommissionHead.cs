using Core.Api.Extensions;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.Entities.TeacherStaff;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using DiplomaControlSystem.Api.Contracts.Common;
using DiplomaControlSystem.Api.Infrastructure.Access;
using DiplomaControlSystem.Api.Infrastructure.CommissionHeads;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using static DiplomaControlSystem.Api.Contracts.CommissionHeads.CommissionHeadContracts;

namespace DiplomaControlSystem.Api.Features.CommissionHeads;

public static class CreateCommissionHead
{
    public sealed record CreateCommissionHeadRequest(
        string FullName,
        PersonNameFormsDto? NameForms,
        string Position,
        string Company,
        string Specialty) : ICommissionHeadRequest;

    internal sealed class Validator : AbstractValidator<CreateCommissionHeadRequest>
    {
        public Validator()
        {
            Include(new CommissionHeadRequestValidator<CreateCommissionHeadRequest>());
        }
    }

    internal static class Endpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/commission-heads", Handle)
                .WithSummary("Creates a commission head")
                .Produces<CommissionHeadDto>(StatusCodes.Status201Created)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status409Conflict)
                .WithTags("Commission Heads");
        }

        private static async Task<Results<Created<CommissionHeadDto>, ProblemHttpResult, ValidationProblem>> Handle(
            [FromBody] CreateCommissionHeadRequest request,
            [FromServices] IValidator<CreateCommissionHeadRequest> validator,
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

            return TypedResults.Created($"/api/commission-heads/{result.Value!.Id}", result.Value);
        }
    }

    private sealed class Handler(
        DbDocGenContext context,
        SecretaryAccessService secretaryAccessService) : IScopedService
    {
        public async Task<Result<CommissionHeadDto>> HandleAsync(
            CreateCommissionHeadRequest request,
            CancellationToken ct)
        {
            var secretaryResult = await secretaryAccessService.GetCurrentSecretaryAsync(ct);
            if (secretaryResult.IsFailure)
            {
                return secretaryResult.ErrorDetails;
            }

            var normalized = CommissionHeadRequestSupport.Normalize(request);

            var duplicateExists = await CommissionHeadRequestSupport.ActiveDuplicateExistsAsync(
                context,
                normalized,
                exceptId: null,
                ct);

            if (duplicateExists)
            {
                return ErrorDetails.Conflict(
                    "CommissionHead.AlreadyExists",
                    "Active commission head with the same data already exists.");
            }

            var commissionHead = new CommissionHead(
                normalized.FullName,
                normalized.Position,
                normalized.Company,
                normalized.Specialty)
            {
                NameForms = request.NameForms?.ToDomain(normalized.FullName)
                    ?? Core.Domain.Entities.PersonNameForms.FromDefault(normalized.FullName)
            };

            await context.CommissionHeads.AddAsync(commissionHead, ct);
            await context.SaveChangesAsync(ct);

            return CommissionHeadRequestSupport.Map(commissionHead);
        }
    }
}
