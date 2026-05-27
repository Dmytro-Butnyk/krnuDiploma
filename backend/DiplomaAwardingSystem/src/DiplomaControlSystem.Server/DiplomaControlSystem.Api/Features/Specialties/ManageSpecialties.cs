using Core.Api.Extensions;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.Entities.StudyGroup;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using DiplomaControlSystem.Api.Infrastructure.Access;
using DiplomaControlSystem.Api.Infrastructure.Auth;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DiplomaControlSystem.Api.Features.Specialties;

public static class ManageSpecialties
{
    public sealed record SpecialtyDto(int Id, string Code, string Name, bool IsActive);
    public sealed record UpsertSpecialtyRequest(string Code, string Name, bool? IsActive);

    internal sealed class Validator : AbstractValidator<UpsertSpecialtyRequest>
    {
        public Validator()
        {
            RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        }
    }

    internal static class Endpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/management/specialties", GetAll)
                .RequireAuthorization(AuthPolicies.SuperSecretary)
                .WithSummary("Gets specialties for management")
                .Produces<IReadOnlyCollection<SpecialtyDto>>()
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .WithTags("Management");

            app.MapPost("/management/specialties", Create)
                .RequireAuthorization(AuthPolicies.SuperSecretary)
                .WithSummary("Creates a specialty")
                .Produces<SpecialtyDto>(StatusCodes.Status201Created)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status409Conflict)
                .WithTags("Management");

            app.MapPut("/management/specialties/{specialtyId:int}", Update)
                .RequireAuthorization(AuthPolicies.SuperSecretary)
                .WithSummary("Updates a specialty")
                .Produces<SpecialtyDto>()
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status409Conflict)
                .WithTags("Management");

            app.MapDelete("/management/specialties/{specialtyId:int}", Deactivate)
                .RequireAuthorization(AuthPolicies.SuperSecretary)
                .WithSummary("Soft deletes a specialty")
                .Produces(StatusCodes.Status204NoContent)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithTags("Management");

            app.MapPost("/management/specialties/{specialtyId:int}/restore", Restore)
                .RequireAuthorization(AuthPolicies.SuperSecretary)
                .WithSummary("Restores a specialty")
                .Produces<SpecialtyDto>()
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithTags("Management");
        }

        private static async Task<Results<Ok<IReadOnlyCollection<SpecialtyDto>>, ProblemHttpResult>> GetAll(
            [FromServices] Handler handler,
            CancellationToken ct)
        {
            var result = await handler.GetAllAsync(ct);
            return result.IsFailure ? result.ToProblemDetails() : TypedResults.Ok(result.Value!);
        }

        private static async Task<Results<Created<SpecialtyDto>, ProblemHttpResult, ValidationProblem>> Create(
            [FromBody] UpsertSpecialtyRequest request,
            [FromServices] IValidator<UpsertSpecialtyRequest> validator,
            [FromServices] Handler handler,
            CancellationToken ct)
        {
            ValidationResult validationResult = await validator.ValidateAsync(request, ct);
            if (!validationResult.IsValid)
            {
                return TypedResults.ValidationProblem(validationResult.ToDictionary());
            }

            var result = await handler.CreateAsync(request, ct);
            return result.IsFailure
                ? result.ToProblemDetails()
                : TypedResults.Created($"/api/management/specialties/{result.Value!.Id}", result.Value);
        }

        private static async Task<Results<Ok<SpecialtyDto>, ProblemHttpResult, ValidationProblem>> Update(
            [FromRoute] int specialtyId,
            [FromBody] UpsertSpecialtyRequest request,
            [FromServices] IValidator<UpsertSpecialtyRequest> validator,
            [FromServices] Handler handler,
            CancellationToken ct)
        {
            ValidationResult validationResult = await validator.ValidateAsync(request, ct);
            if (!validationResult.IsValid)
            {
                return TypedResults.ValidationProblem(validationResult.ToDictionary());
            }

            var result = await handler.UpdateAsync(specialtyId, request, ct);
            return result.IsFailure ? result.ToProblemDetails() : TypedResults.Ok(result.Value!);
        }

        private static async Task<Results<NoContent, ProblemHttpResult>> Deactivate(
            [FromRoute] int specialtyId,
            [FromServices] Handler handler,
            CancellationToken ct)
        {
            var result = await handler.SetActiveAsync(specialtyId, isActive: false, ct);
            return result.IsFailure ? result.ToProblemDetails() : TypedResults.NoContent();
        }

        private static async Task<Results<Ok<SpecialtyDto>, ProblemHttpResult>> Restore(
            [FromRoute] int specialtyId,
            [FromServices] Handler handler,
            CancellationToken ct)
        {
            var result = await handler.SetActiveAsync(specialtyId, isActive: true, ct);
            return result.IsFailure ? result.ToProblemDetails() : TypedResults.Ok(result.Value!);
        }
    }

    private sealed class Handler(
        DbDocGenContext context,
        SecretaryAccessService secretaryAccessService) : IScopedService
    {
        public async Task<Result<IReadOnlyCollection<SpecialtyDto>>> GetAllAsync(CancellationToken ct)
        {
            var guard = await secretaryAccessService.GetCurrentSuperSecretaryAsync(ct);
            if (guard.IsFailure)
            {
                return guard.ErrorDetails;
            }

            return await context.Specialties
                .AsNoTracking()
                .OrderBy(s => s.Code)
                .Select(s => Map(s))
                .ToListAsync(ct);
        }

        public async Task<Result<SpecialtyDto>> CreateAsync(UpsertSpecialtyRequest request, CancellationToken ct)
        {
            var guard = await secretaryAccessService.GetCurrentSuperSecretaryAsync(ct);
            if (guard.IsFailure)
            {
                return guard.ErrorDetails;
            }

            var code = request.Code.Trim();
            if (await context.Specialties.AnyAsync(s => s.Code == code, ct))
            {
                return ErrorDetails.Conflict("Specialty.AlreadyExists", "Specialty with the same code already exists.");
            }

            var specialty = new Specialty(code, request.Name.Trim())
            {
                IsActive = request.IsActive ?? true
            };

            await context.Specialties.AddAsync(specialty, ct);
            await context.SaveChangesAsync(ct);
            return Map(specialty);
        }

        public async Task<Result<SpecialtyDto>> UpdateAsync(
            int specialtyId,
            UpsertSpecialtyRequest request,
            CancellationToken ct)
        {
            var guard = await secretaryAccessService.GetCurrentSuperSecretaryAsync(ct);
            if (guard.IsFailure)
            {
                return guard.ErrorDetails;
            }

            var specialty = await context.Specialties.FirstOrDefaultAsync(s => s.Id == specialtyId, ct);
            if (specialty is null)
            {
                return ErrorDetails.NotFound("Specialty.NotFound", "Specialty was not found.");
            }

            var code = request.Code.Trim();
            if (await context.Specialties.AnyAsync(s => s.Id != specialtyId && s.Code == code, ct))
            {
                return ErrorDetails.Conflict("Specialty.AlreadyExists", "Specialty with the same code already exists.");
            }

            specialty.Code = code;
            specialty.Name = request.Name.Trim();
            specialty.IsActive = request.IsActive ?? specialty.IsActive;

            await context.SaveChangesAsync(ct);
            return Map(specialty);
        }

        public async Task<Result<SpecialtyDto>> SetActiveAsync(
            int specialtyId,
            bool isActive,
            CancellationToken ct)
        {
            var guard = await secretaryAccessService.GetCurrentSuperSecretaryAsync(ct);
            if (guard.IsFailure)
            {
                return guard.ErrorDetails;
            }

            var specialty = await context.Specialties.FirstOrDefaultAsync(s => s.Id == specialtyId, ct);
            if (specialty is null)
            {
                return ErrorDetails.NotFound("Specialty.NotFound", "Specialty was not found.");
            }

            specialty.IsActive = isActive;
            await context.SaveChangesAsync(ct);
            return Map(specialty);
        }

        private static SpecialtyDto Map(Specialty specialty)
        {
            return new SpecialtyDto(specialty.Id, specialty.Code, specialty.Name, specialty.IsActive);
        }
    }
}
