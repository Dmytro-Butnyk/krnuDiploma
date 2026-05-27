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

namespace DiplomaControlSystem.Api.Features.AcademicDegrees;

public static class ManageAcademicDegrees
{
    public sealed record AcademicDegreeDto(int Id, string FullName, string ShortName, bool IsActive);
    public sealed record UpsertAcademicDegreeRequest(string FullName, string ShortName, bool? IsActive);

    internal sealed class Validator : AbstractValidator<UpsertAcademicDegreeRequest>
    {
        public Validator()
        {
            RuleFor(x => x.FullName).NotEmpty().MaximumLength(256);
            RuleFor(x => x.ShortName).NotEmpty().MaximumLength(50);
        }
    }

    internal static class Endpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/management/academic-degrees", GetAll).RequireAuthorization(AuthPolicies.SuperSecretary)
                .WithSummary("Gets academic degrees for management").Produces<IReadOnlyCollection<AcademicDegreeDto>>().WithTags("Management");
            app.MapPost("/management/academic-degrees", Create).RequireAuthorization(AuthPolicies.SuperSecretary)
                .WithSummary("Creates an academic degree").Produces<AcademicDegreeDto>(StatusCodes.Status201Created).ProducesValidationProblem().ProducesProblem(StatusCodes.Status409Conflict).WithTags("Management");
            app.MapPut("/management/academic-degrees/{degreeId:int}", Update).RequireAuthorization(AuthPolicies.SuperSecretary)
                .WithSummary("Updates an academic degree").Produces<AcademicDegreeDto>().ProducesValidationProblem().ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict).WithTags("Management");
            app.MapDelete("/management/academic-degrees/{degreeId:int}", Deactivate).RequireAuthorization(AuthPolicies.SuperSecretary)
                .WithSummary("Soft deletes an academic degree").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status404NotFound).WithTags("Management");
            app.MapPost("/management/academic-degrees/{degreeId:int}/restore", Restore).RequireAuthorization(AuthPolicies.SuperSecretary)
                .WithSummary("Restores an academic degree").Produces<AcademicDegreeDto>().ProducesProblem(StatusCodes.Status404NotFound).WithTags("Management");
        }

        private static async Task<Results<Ok<IReadOnlyCollection<AcademicDegreeDto>>, ProblemHttpResult>> GetAll([FromServices] Handler handler, CancellationToken ct)
        {
            var result = await handler.GetAllAsync(ct);
            return result.IsFailure ? result.ToProblemDetails() : TypedResults.Ok(result.Value!);
        }

        private static async Task<Results<Created<AcademicDegreeDto>, ProblemHttpResult, ValidationProblem>> Create(
            [FromBody] UpsertAcademicDegreeRequest request,
            [FromServices] IValidator<UpsertAcademicDegreeRequest> validator,
            [FromServices] Handler handler,
            CancellationToken ct)
        {
            ValidationResult validationResult = await validator.ValidateAsync(request, ct);
            if (!validationResult.IsValid)
            {
                return TypedResults.ValidationProblem(validationResult.ToDictionary());
            }

            var result = await handler.CreateAsync(request, ct);
            return result.IsFailure ? result.ToProblemDetails() : TypedResults.Created($"/api/management/academic-degrees/{result.Value!.Id}", result.Value);
        }

        private static async Task<Results<Ok<AcademicDegreeDto>, ProblemHttpResult, ValidationProblem>> Update(
            [FromRoute] int degreeId,
            [FromBody] UpsertAcademicDegreeRequest request,
            [FromServices] IValidator<UpsertAcademicDegreeRequest> validator,
            [FromServices] Handler handler,
            CancellationToken ct)
        {
            ValidationResult validationResult = await validator.ValidateAsync(request, ct);
            if (!validationResult.IsValid)
            {
                return TypedResults.ValidationProblem(validationResult.ToDictionary());
            }

            var result = await handler.UpdateAsync(degreeId, request, ct);
            return result.IsFailure ? result.ToProblemDetails() : TypedResults.Ok(result.Value!);
        }

        private static async Task<Results<NoContent, ProblemHttpResult>> Deactivate([FromRoute] int degreeId, [FromServices] Handler handler, CancellationToken ct)
        {
            var result = await handler.SetActiveAsync(degreeId, isActive: false, ct);
            return result.IsFailure ? result.ToProblemDetails() : TypedResults.NoContent();
        }

        private static async Task<Results<Ok<AcademicDegreeDto>, ProblemHttpResult>> Restore([FromRoute] int degreeId, [FromServices] Handler handler, CancellationToken ct)
        {
            var result = await handler.SetActiveAsync(degreeId, isActive: true, ct);
            return result.IsFailure ? result.ToProblemDetails() : TypedResults.Ok(result.Value!);
        }
    }

    private sealed class Handler(DbDocGenContext context, SecretaryAccessService secretaryAccessService) : IScopedService
    {
        public async Task<Result<IReadOnlyCollection<AcademicDegreeDto>>> GetAllAsync(CancellationToken ct)
        {
            var guard = await secretaryAccessService.GetCurrentSuperSecretaryAsync(ct);
            if (guard.IsFailure)
            {
                return guard.ErrorDetails;
            }

            return await context.AcademicDegrees.AsNoTracking().OrderBy(d => d.FullName).Select(d => Map(d)).ToListAsync(ct);
        }

        public async Task<Result<AcademicDegreeDto>> CreateAsync(UpsertAcademicDegreeRequest request, CancellationToken ct)
        {
            var guard = await secretaryAccessService.GetCurrentSuperSecretaryAsync(ct);
            if (guard.IsFailure)
            {
                return guard.ErrorDetails;
            }

            var fullName = request.FullName.Trim();
            var shortName = request.ShortName.Trim();
            if (await context.AcademicDegrees.AnyAsync(d => d.FullName == fullName || d.ShortName == shortName, ct))
            {
                return ErrorDetails.Conflict("AcademicDegree.AlreadyExists", "Academic degree with the same name already exists.");
            }

            var degree = new AcademicDegree(fullName, shortName) { IsActive = request.IsActive ?? true };
            await context.AcademicDegrees.AddAsync(degree, ct);
            await context.SaveChangesAsync(ct);
            return Map(degree);
        }

        public async Task<Result<AcademicDegreeDto>> UpdateAsync(int degreeId, UpsertAcademicDegreeRequest request, CancellationToken ct)
        {
            var guard = await secretaryAccessService.GetCurrentSuperSecretaryAsync(ct);
            if (guard.IsFailure)
            {
                return guard.ErrorDetails;
            }

            var degree = await context.AcademicDegrees.FirstOrDefaultAsync(d => d.Id == degreeId, ct);
            if (degree is null)
            {
                return ErrorDetails.NotFound("AcademicDegree.NotFound", "Academic degree was not found.");
            }

            var fullName = request.FullName.Trim();
            var shortName = request.ShortName.Trim();
            if (await context.AcademicDegrees.AnyAsync(d => d.Id != degreeId && (d.FullName == fullName || d.ShortName == shortName), ct))
            {
                return ErrorDetails.Conflict("AcademicDegree.AlreadyExists", "Academic degree with the same name already exists.");
            }

            degree.FullName = fullName;
            degree.ShortName = shortName;
            degree.IsActive = request.IsActive ?? degree.IsActive;
            await context.SaveChangesAsync(ct);
            return Map(degree);
        }

        public async Task<Result<AcademicDegreeDto>> SetActiveAsync(int degreeId, bool isActive, CancellationToken ct)
        {
            var guard = await secretaryAccessService.GetCurrentSuperSecretaryAsync(ct);
            if (guard.IsFailure)
            {
                return guard.ErrorDetails;
            }

            var degree = await context.AcademicDegrees.FirstOrDefaultAsync(d => d.Id == degreeId, ct);
            if (degree is null)
            {
                return ErrorDetails.NotFound("AcademicDegree.NotFound", "Academic degree was not found.");
            }

            degree.IsActive = isActive;
            await context.SaveChangesAsync(ct);
            return Map(degree);
        }

        private static AcademicDegreeDto Map(AcademicDegree degree)
        {
            return new AcademicDegreeDto(degree.Id, degree.FullName, degree.ShortName, degree.IsActive);
        }
    }
}
