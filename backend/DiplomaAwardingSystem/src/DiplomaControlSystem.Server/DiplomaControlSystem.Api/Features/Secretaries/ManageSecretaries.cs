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

namespace DiplomaControlSystem.Api.Features.Secretaries;

public static class ManageSecretaries
{
    public sealed record SecretaryDto(
        int Id,
        string Email,
        string FullName,
        int SpecialtyId,
        string SpecialtyName,
        bool IsActive,
        bool IsSuperSecretary,
        bool IsGoogleLinked);

    public sealed record UpsertSecretaryRequest(
        string Email,
        string FullName,
        int SpecialtyId,
        bool IsActive,
        bool IsSuperSecretary);

    public sealed record SetSuperSecretaryRoleRequest(bool IsSuperSecretary);

    internal sealed class UpsertValidator : AbstractValidator<UpsertSecretaryRequest>
    {
        public UpsertValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
            RuleFor(x => x.FullName).NotEmpty().MaximumLength(256);
            RuleFor(x => x.SpecialtyId).GreaterThan(0);
        }
    }

    internal static class Endpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/management/secretaries", GetAll).RequireAuthorization(AuthPolicies.SuperSecretary)
                .WithSummary("Gets secretaries for management").Produces<IReadOnlyCollection<SecretaryDto>>().WithTags("Management");
            app.MapPost("/management/secretaries", Create).RequireAuthorization(AuthPolicies.SuperSecretary)
                .WithSummary("Registers a secretary account").Produces<SecretaryDto>(StatusCodes.Status201Created).ProducesValidationProblem().ProducesProblem(StatusCodes.Status409Conflict).WithTags("Management");
            app.MapPut("/management/secretaries/{secretaryId:int}", Update).RequireAuthorization(AuthPolicies.SuperSecretary)
                .WithSummary("Updates a secretary account").Produces<SecretaryDto>().ProducesValidationProblem().ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict).WithTags("Management");
            app.MapDelete("/management/secretaries/{secretaryId:int}", Deactivate).RequireAuthorization(AuthPolicies.SuperSecretary)
                .WithSummary("Soft deletes a secretary account").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict).WithTags("Management");
            app.MapPost("/management/secretaries/{secretaryId:int}/restore", Restore).RequireAuthorization(AuthPolicies.SuperSecretary)
                .WithSummary("Restores a secretary account").Produces<SecretaryDto>().ProducesProblem(StatusCodes.Status404NotFound).WithTags("Management");
            app.MapDelete("/management/secretaries/{secretaryId:int}/hard-delete", HardDelete).RequireAuthorization(AuthPolicies.SuperSecretary)
                .WithSummary("Permanently deletes an unused secretary account").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict).WithTags("Management");
            app.MapPatch("/management/secretaries/{secretaryId:int}/super-role", SetSuperRole).RequireAuthorization(AuthPolicies.SuperSecretary)
                .WithSummary("Adds or removes super secretary role").Produces<SecretaryDto>().ProducesValidationProblem().ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict).WithTags("Management");
        }

        private static async Task<Results<Ok<IReadOnlyCollection<SecretaryDto>>, ProblemHttpResult>> GetAll([FromServices] Handler handler, CancellationToken ct)
        {
            var result = await handler.GetAllAsync(ct);
            return result.IsFailure ? result.ToProblemDetails() : TypedResults.Ok(result.Value!);
        }

        private static async Task<Results<Created<SecretaryDto>, ProblemHttpResult, ValidationProblem>> Create(
            [FromBody] UpsertSecretaryRequest request,
            [FromServices] IValidator<UpsertSecretaryRequest> validator,
            [FromServices] Handler handler,
            CancellationToken ct)
        {
            ValidationResult validationResult = await validator.ValidateAsync(request, ct);
            if (!validationResult.IsValid)
            {
                return TypedResults.ValidationProblem(validationResult.ToDictionary());
            }

            var result = await handler.CreateAsync(request, ct);
            return result.IsFailure ? result.ToProblemDetails() : TypedResults.Created($"/api/management/secretaries/{result.Value!.Id}", result.Value);
        }

        private static async Task<Results<Ok<SecretaryDto>, ProblemHttpResult, ValidationProblem>> Update(
            [FromRoute] int secretaryId,
            [FromBody] UpsertSecretaryRequest request,
            [FromServices] IValidator<UpsertSecretaryRequest> validator,
            [FromServices] Handler handler,
            CancellationToken ct)
        {
            ValidationResult validationResult = await validator.ValidateAsync(request, ct);
            if (!validationResult.IsValid)
            {
                return TypedResults.ValidationProblem(validationResult.ToDictionary());
            }

            var result = await handler.UpdateAsync(secretaryId, request, ct);
            return result.IsFailure ? result.ToProblemDetails() : TypedResults.Ok(result.Value!);
        }

        private static async Task<Results<NoContent, ProblemHttpResult>> Deactivate([FromRoute] int secretaryId, [FromServices] Handler handler, CancellationToken ct)
        {
            var result = await handler.SetActiveAsync(secretaryId, isActive: false, ct);
            return result.IsFailure ? result.ToProblemDetails() : TypedResults.NoContent();
        }

        private static async Task<Results<Ok<SecretaryDto>, ProblemHttpResult>> Restore([FromRoute] int secretaryId, [FromServices] Handler handler, CancellationToken ct)
        {
            var result = await handler.SetActiveAsync(secretaryId, isActive: true, ct);
            return result.IsFailure ? result.ToProblemDetails() : TypedResults.Ok(result.Value!);
        }

        private static async Task<Results<NoContent, ProblemHttpResult>> HardDelete([FromRoute] int secretaryId, [FromServices] Handler handler, CancellationToken ct)
        {
            var result = await handler.HardDeleteAsync(secretaryId, ct);
            return result.IsFailure ? result.ToProblemDetails() : TypedResults.NoContent();
        }

        private static async Task<Results<Ok<SecretaryDto>, ProblemHttpResult>> SetSuperRole(
            [FromRoute] int secretaryId,
            [FromBody] SetSuperSecretaryRoleRequest request,
            [FromServices] Handler handler,
            CancellationToken ct)
        {
            var result = await handler.SetSuperRoleAsync(secretaryId, request.IsSuperSecretary, ct);
            return result.IsFailure ? result.ToProblemDetails() : TypedResults.Ok(result.Value!);
        }
    }

    private sealed class Handler(DbDocGenContext context, SecretaryAccessService secretaryAccessService) : IScopedService
    {
        public async Task<Result<IReadOnlyCollection<SecretaryDto>>> GetAllAsync(CancellationToken ct)
        {
            var guard = await secretaryAccessService.GetCurrentSuperSecretaryAsync(ct);
            if (guard.IsFailure)
            {
                return guard.ErrorDetails;
            }

            return await context.Secretaries
                .AsNoTracking()
                .OrderBy(s => s.FullName)
                .Select(s => new SecretaryDto(
                    s.Id,
                    s.Email,
                    s.FullName,
                    s.SpecialtyId,
                    s.Specialty!.Name,
                    s.IsActive,
                    s.IsSuperSecretary,
                    s.GoogleSubject != null))
                .ToListAsync(ct);
        }

        public async Task<Result<SecretaryDto>> CreateAsync(UpsertSecretaryRequest request, CancellationToken ct)
        {
            var guard = await secretaryAccessService.GetCurrentSuperSecretaryAsync(ct);
            if (guard.IsFailure)
            {
                return guard.ErrorDetails;
            }

            var dependencyValidation = await ValidateSpecialtyAsync(request.SpecialtyId, ct);
            if (dependencyValidation.IsFailure)
            {
                return dependencyValidation.ErrorDetails;
            }

            var email = request.Email.Trim();
            if (await context.Secretaries.AnyAsync(s => s.Email == email, ct))
            {
                return ErrorDetails.Conflict("Secretary.AlreadyExists", "Secretary with the same email already exists.");
            }

            var secretary = new Secretary(email, request.FullName.Trim(), request.SpecialtyId, request.IsActive, request.IsSuperSecretary);
            await context.Secretaries.AddAsync(secretary, ct);
            await context.SaveChangesAsync(ct);
            return await GetByIdAsync(secretary.Id, ct);
        }

        public async Task<Result<SecretaryDto>> UpdateAsync(int secretaryId, UpsertSecretaryRequest request, CancellationToken ct)
        {
            var guard = await secretaryAccessService.GetCurrentSuperSecretaryAsync(ct);
            if (guard.IsFailure)
            {
                return guard.ErrorDetails;
            }

            var dependencyValidation = await ValidateSpecialtyAsync(request.SpecialtyId, ct);
            if (dependencyValidation.IsFailure)
            {
                return dependencyValidation.ErrorDetails;
            }

            var secretary = await context.Secretaries.FirstOrDefaultAsync(s => s.Id == secretaryId, ct);
            if (secretary is null)
            {
                return ErrorDetails.NotFound("Secretary.NotFound", "Secretary was not found.");
            }

            var selfProtection = ValidateSelfProtection(guard.Value!, secretaryId, request.IsActive, request.IsSuperSecretary);
            if (selfProtection.IsFailure)
            {
                return selfProtection.ErrorDetails;
            }

            var email = request.Email.Trim();
            if (await context.Secretaries.AnyAsync(s => s.Id != secretaryId && s.Email == email, ct))
            {
                return ErrorDetails.Conflict("Secretary.AlreadyExists", "Secretary with the same email already exists.");
            }

            secretary.Email = email;
            secretary.FullName = request.FullName.Trim();
            secretary.SpecialtyId = request.SpecialtyId;
            secretary.IsActive = request.IsActive;
            secretary.IsSuperSecretary = request.IsSuperSecretary;

            await context.SaveChangesAsync(ct);
            return await GetByIdAsync(secretary.Id, ct);
        }

        public async Task<Result<SecretaryDto>> SetActiveAsync(int secretaryId, bool isActive, CancellationToken ct)
        {
            var guard = await secretaryAccessService.GetCurrentSuperSecretaryAsync(ct);
            if (guard.IsFailure)
            {
                return guard.ErrorDetails;
            }

            if (guard.Value!.SecretaryId == secretaryId && !isActive)
            {
                return ErrorDetails.Conflict("Secretary.SelfDeactivation", "Current super secretary cannot deactivate own account.");
            }

            var secretary = await context.Secretaries.FirstOrDefaultAsync(s => s.Id == secretaryId, ct);
            if (secretary is null)
            {
                return ErrorDetails.NotFound("Secretary.NotFound", "Secretary was not found.");
            }

            secretary.IsActive = isActive;
            await context.SaveChangesAsync(ct);
            return await GetByIdAsync(secretary.Id, ct);
        }

        public async Task<Result<SecretaryDto>> SetSuperRoleAsync(int secretaryId, bool isSuperSecretary, CancellationToken ct)
        {
            var guard = await secretaryAccessService.GetCurrentSuperSecretaryAsync(ct);
            if (guard.IsFailure)
            {
                return guard.ErrorDetails;
            }

            if (guard.Value!.SecretaryId == secretaryId && !isSuperSecretary)
            {
                return ErrorDetails.Conflict("Secretary.SelfRoleRemoval", "Current super secretary cannot remove own super role.");
            }

            var secretary = await context.Secretaries.FirstOrDefaultAsync(s => s.Id == secretaryId, ct);
            if (secretary is null)
            {
                return ErrorDetails.NotFound("Secretary.NotFound", "Secretary was not found.");
            }

            secretary.IsSuperSecretary = isSuperSecretary;
            await context.SaveChangesAsync(ct);
            return await GetByIdAsync(secretary.Id, ct);
        }

        public async Task<Result> HardDeleteAsync(int secretaryId, CancellationToken ct)
        {
            var guard = await secretaryAccessService.GetCurrentSuperSecretaryAsync(ct);
            if (guard.IsFailure)
            {
                return guard.ErrorDetails;
            }

            if (guard.Value!.SecretaryId == secretaryId)
            {
                return ErrorDetails.Conflict("Secretary.SelfDeletion", "Current super secretary cannot delete own account.");
            }

            var secretary = await context.Secretaries.FirstOrDefaultAsync(s => s.Id == secretaryId, ct);
            if (secretary is null)
            {
                return ErrorDetails.NotFound("Secretary.NotFound", "Secretary was not found.");
            }

            var usedInCommissions = await context.DiplomaExaminationCommissions.AnyAsync(c => c.SecretaryId == secretaryId, ct);
            if (usedInCommissions)
            {
                return ErrorDetails.Conflict("Secretary.InUse", "Secretary is used in existing diploma examination commissions.");
            }

            context.Secretaries.Remove(secretary);
            await context.SaveChangesAsync(ct);
            return Result.Success();
        }

        private async Task<Result> ValidateSpecialtyAsync(int specialtyId, CancellationToken ct)
        {
            if (!await context.Specialties.AnyAsync(s => s.Id == specialtyId && s.IsActive, ct))
            {
                return ErrorDetails.NotFound("Specialty.NotFound", "Active specialty was not found.");
            }

            return Result.Success();
        }

        private async Task<Result<SecretaryDto>> GetByIdAsync(int secretaryId, CancellationToken ct)
        {
            var secretary = await context.Secretaries
                .AsNoTracking()
                .Where(s => s.Id == secretaryId)
                .Select(s => new SecretaryDto(
                    s.Id,
                    s.Email,
                    s.FullName,
                    s.SpecialtyId,
                    s.Specialty!.Name,
                    s.IsActive,
                    s.IsSuperSecretary,
                    s.GoogleSubject != null))
                .FirstOrDefaultAsync(ct);

            return secretary is null
                ? ErrorDetails.NotFound("Secretary.NotFound", "Secretary was not found.")
                : secretary;
        }

        private static Result ValidateSelfProtection(
            SecretaryAccessContext current,
            int targetSecretaryId,
            bool targetIsActive,
            bool targetIsSuperSecretary)
        {
            if (current.SecretaryId != targetSecretaryId)
            {
                return Result.Success();
            }

            if (!targetIsActive)
            {
                return ErrorDetails.Conflict("Secretary.SelfDeactivation", "Current super secretary cannot deactivate own account.");
            }

            if (!targetIsSuperSecretary)
            {
                return ErrorDetails.Conflict("Secretary.SelfRoleRemoval", "Current super secretary cannot remove own super role.");
            }

            return Result.Success();
        }
    }
}
