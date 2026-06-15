using Core.Api.Extensions;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.Entities.TeacherStaff;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using DiplomaControlSystem.Api.Infrastructure.Access;
using DiplomaControlSystem.Api.Infrastructure.Auth;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DiplomaControlSystem.Api.Features.TeacherPositions;

public static class ManageTeacherPositions
{
    public sealed record TeacherPositionDto(
        int Id,
        string FullName,
        string ShortName,
        string GenitiveFullName,
        string GenitiveShortName,
        bool IsActive);

    public sealed record UpsertTeacherPositionRequest(
        string FullName,
        string ShortName,
        string? GenitiveFullName,
        string? GenitiveShortName,
        bool? IsActive);

    internal sealed class Validator : AbstractValidator<UpsertTeacherPositionRequest>
    {
        public Validator()
        {
            RuleFor(x => x.FullName).NotEmpty().MaximumLength(256);
            RuleFor(x => x.ShortName).NotEmpty().MaximumLength(256);
            RuleFor(x => x.GenitiveFullName).MaximumLength(256);
            RuleFor(x => x.GenitiveShortName).MaximumLength(256);
        }
    }

    internal static class Endpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/management/teacher-positions", GetAll).RequireAuthorization(AuthPolicies.SuperSecretary)
                .WithSummary("Gets teacher positions for management").Produces<IReadOnlyCollection<TeacherPositionDto>>().WithTags("Management");
            app.MapPost("/management/teacher-positions", Create).RequireAuthorization(AuthPolicies.SuperSecretary)
                .WithSummary("Creates a teacher position").Produces<TeacherPositionDto>(StatusCodes.Status201Created).ProducesValidationProblem().ProducesProblem(StatusCodes.Status409Conflict).WithTags("Management");
            app.MapPut("/management/teacher-positions/{positionId:int}", Update).RequireAuthorization(AuthPolicies.SuperSecretary)
                .WithSummary("Updates a teacher position").Produces<TeacherPositionDto>().ProducesValidationProblem().ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict).WithTags("Management");
            app.MapDelete("/management/teacher-positions/{positionId:int}", Deactivate).RequireAuthorization(AuthPolicies.SuperSecretary)
                .WithSummary("Soft deletes a teacher position").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status404NotFound).WithTags("Management");
            app.MapPost("/management/teacher-positions/{positionId:int}/restore", Restore).RequireAuthorization(AuthPolicies.SuperSecretary)
                .WithSummary("Restores a teacher position").Produces<TeacherPositionDto>().ProducesProblem(StatusCodes.Status404NotFound).WithTags("Management");
        }

        private static async Task<Results<Ok<IReadOnlyCollection<TeacherPositionDto>>, ProblemHttpResult>> GetAll([FromServices] Handler handler, CancellationToken ct)
        {
            var result = await handler.GetAllAsync(ct);
            return result.IsFailure ? result.ToProblemDetails() : TypedResults.Ok(result.Value!);
        }

        private static async Task<Results<Created<TeacherPositionDto>, ProblemHttpResult, ValidationProblem>> Create(
            [FromBody] UpsertTeacherPositionRequest request,
            [FromServices] IValidator<UpsertTeacherPositionRequest> validator,
            [FromServices] Handler handler,
            CancellationToken ct)
        {
            ValidationResult validationResult = await validator.ValidateAsync(request, ct);
            if (!validationResult.IsValid)
            {
                return TypedResults.ValidationProblem(validationResult.ToDictionary());
            }

            var result = await handler.CreateAsync(request, ct);
            return result.IsFailure ? result.ToProblemDetails() : TypedResults.Created($"/api/management/teacher-positions/{result.Value!.Id}", result.Value);
        }

        private static async Task<Results<Ok<TeacherPositionDto>, ProblemHttpResult, ValidationProblem>> Update(
            [FromRoute] int positionId,
            [FromBody] UpsertTeacherPositionRequest request,
            [FromServices] IValidator<UpsertTeacherPositionRequest> validator,
            [FromServices] Handler handler,
            CancellationToken ct)
        {
            ValidationResult validationResult = await validator.ValidateAsync(request, ct);
            if (!validationResult.IsValid)
            {
                return TypedResults.ValidationProblem(validationResult.ToDictionary());
            }

            var result = await handler.UpdateAsync(positionId, request, ct);
            return result.IsFailure ? result.ToProblemDetails() : TypedResults.Ok(result.Value!);
        }

        private static async Task<Results<NoContent, ProblemHttpResult>> Deactivate([FromRoute] int positionId, [FromServices] Handler handler, CancellationToken ct)
        {
            var result = await handler.SetActiveAsync(positionId, isActive: false, ct);
            return result.IsFailure ? result.ToProblemDetails() : TypedResults.NoContent();
        }

        private static async Task<Results<Ok<TeacherPositionDto>, ProblemHttpResult>> Restore([FromRoute] int positionId, [FromServices] Handler handler, CancellationToken ct)
        {
            var result = await handler.SetActiveAsync(positionId, isActive: true, ct);
            return result.IsFailure ? result.ToProblemDetails() : TypedResults.Ok(result.Value!);
        }
    }

    private sealed class Handler(DbDocGenContext context, SecretaryAccessService secretaryAccessService) : IScopedService
    {
        public async Task<Result<IReadOnlyCollection<TeacherPositionDto>>> GetAllAsync(CancellationToken ct)
        {
            var guard = await secretaryAccessService.GetCurrentSuperSecretaryAsync(ct);
            if (guard.IsFailure)
            {
                return guard.ErrorDetails;
            }

            return await context.TeacherPositions.AsNoTracking().OrderBy(p => p.FullName).Select(p => Map(p)).ToListAsync(ct);
        }

        public async Task<Result<TeacherPositionDto>> CreateAsync(UpsertTeacherPositionRequest request, CancellationToken ct)
        {
            var guard = await secretaryAccessService.GetCurrentSuperSecretaryAsync(ct);
            if (guard.IsFailure)
            {
                return guard.ErrorDetails;
            }

            var fullName = request.FullName.Trim();
            var shortName = request.ShortName.Trim();
            if (await context.TeacherPositions.AnyAsync(p => p.FullName == fullName || p.ShortName == shortName, ct))
            {
                return ErrorDetails.Conflict("TeacherPosition.AlreadyExists", "Teacher position with the same name already exists.");
            }

            var position = new TeacherPosition(fullName, shortName)
            {
                GenitiveFullName = NormalizeOptional(request.GenitiveFullName, fullName),
                GenitiveShortName = NormalizeOptional(request.GenitiveShortName, shortName),
                IsActive = request.IsActive ?? true
            };
            await context.TeacherPositions.AddAsync(position, ct);
            await context.SaveChangesAsync(ct);
            return Map(position);
        }

        public async Task<Result<TeacherPositionDto>> UpdateAsync(int positionId, UpsertTeacherPositionRequest request, CancellationToken ct)
        {
            var guard = await secretaryAccessService.GetCurrentSuperSecretaryAsync(ct);
            if (guard.IsFailure)
            {
                return guard.ErrorDetails;
            }

            var position = await context.TeacherPositions.FirstOrDefaultAsync(p => p.Id == positionId, ct);
            if (position is null)
            {
                return ErrorDetails.NotFound("TeacherPosition.NotFound", "Teacher position was not found.");
            }

            var fullName = request.FullName.Trim();
            var shortName = request.ShortName.Trim();
            if (await context.TeacherPositions.AnyAsync(p => p.Id != positionId && (p.FullName == fullName || p.ShortName == shortName), ct))
            {
                return ErrorDetails.Conflict("TeacherPosition.AlreadyExists", "Teacher position with the same name already exists.");
            }

            position.FullName = fullName;
            position.ShortName = shortName;
            position.GenitiveFullName = NormalizeOptional(request.GenitiveFullName, fullName);
            position.GenitiveShortName = NormalizeOptional(request.GenitiveShortName, shortName);
            position.IsActive = request.IsActive ?? position.IsActive;
            await context.SaveChangesAsync(ct);
            return Map(position);
        }

        public async Task<Result<TeacherPositionDto>> SetActiveAsync(int positionId, bool isActive, CancellationToken ct)
        {
            var guard = await secretaryAccessService.GetCurrentSuperSecretaryAsync(ct);
            if (guard.IsFailure)
            {
                return guard.ErrorDetails;
            }

            var position = await context.TeacherPositions.FirstOrDefaultAsync(p => p.Id == positionId, ct);
            if (position is null)
            {
                return ErrorDetails.NotFound("TeacherPosition.NotFound", "Teacher position was not found.");
            }

            position.IsActive = isActive;
            await context.SaveChangesAsync(ct);
            return Map(position);
        }

        private static TeacherPositionDto Map(TeacherPosition position)
        {
            return new TeacherPositionDto(
                position.Id,
                position.FullName,
                position.ShortName,
                position.GenitiveFullName,
                position.GenitiveShortName,
                position.IsActive);
        }

        private static string NormalizeOptional(string? value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }
    }
}
