using Core.Api.Extensions;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.Entities.TeacherStaff;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using DiplomaControlSystem.Api.Infrastructure.Access;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static DiplomaControlSystem.Api.Contracts.DiplomaExaminationCommissions.DiplomaExaminationCommissionContracts;

namespace DiplomaControlSystem.Api.Features.CommissionHeads;

public static class CreateCommissionHead
{
    public sealed record CreateCommissionHeadRequest(
        string SecretaryEmail,
        string FullName,
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
            var secretaryResult = await secretaryAccessService.GetActiveSecretaryAsync(request.SecretaryEmail, ct);
            if (secretaryResult.IsFailure)
            {
                return secretaryResult.ErrorDetails;
            }

            var secretary = secretaryResult.Value!;
            var normalized = CommissionHeadRequestSupport.Normalize(request);

            var specialtyResult = CommissionHeadRequestSupport.ValidateSpecialty(normalized.Specialty, secretary.SpecialtyName);
            if (specialtyResult.IsFailure)
            {
                return specialtyResult.ErrorDetails;
            }

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
                normalized.Specialty);

            await context.CommissionHeads.AddAsync(commissionHead, ct);
            await context.SaveChangesAsync(ct);

            return CommissionHeadRequestSupport.Map(commissionHead);
        }
    }
}
