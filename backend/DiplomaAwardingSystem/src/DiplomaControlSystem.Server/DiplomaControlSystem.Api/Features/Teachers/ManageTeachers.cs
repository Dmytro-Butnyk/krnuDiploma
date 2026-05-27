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

namespace DiplomaControlSystem.Api.Features.Teachers;

public static class ManageTeachers
{
    public sealed record TeacherDto(
        int Id,
        string FullName,
        string ShortName,
        string Email,
        string PhoneNumber,
        int AcademicDegreeId,
        string AcademicDegree,
        int TeacherPositionId,
        string TeacherPosition,
        int SpecialtyId,
        string Specialty,
        bool IsActive);

    public sealed record UpsertTeacherRequest(
        string FullName,
        string ShortName,
        string Email,
        string PhoneNumber,
        int AcademicDegreeId,
        int TeacherPositionId,
        int SpecialtyId,
        bool? IsActive);

    internal sealed class Validator : AbstractValidator<UpsertTeacherRequest>
    {
        public Validator()
        {
            RuleFor(x => x.FullName).NotEmpty().MaximumLength(256);
            RuleFor(x => x.ShortName).NotEmpty().MaximumLength(256);
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(150);
            RuleFor(x => x.PhoneNumber).MaximumLength(50);
            RuleFor(x => x.AcademicDegreeId).GreaterThan(0);
            RuleFor(x => x.TeacherPositionId).GreaterThan(0);
            RuleFor(x => x.SpecialtyId).GreaterThan(0);
        }
    }

    internal static class Endpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/management/teachers", GetAll).RequireAuthorization(AuthPolicies.SuperSecretary)
                .WithSummary("Gets teachers for management").Produces<IReadOnlyCollection<TeacherDto>>().WithTags("Management");
            app.MapPost("/management/teachers", Create).RequireAuthorization(AuthPolicies.SuperSecretary)
                .WithSummary("Creates a teacher").Produces<TeacherDto>(StatusCodes.Status201Created).ProducesValidationProblem().ProducesProblem(StatusCodes.Status409Conflict).WithTags("Management");
            app.MapPut("/management/teachers/{teacherId:int}", Update).RequireAuthorization(AuthPolicies.SuperSecretary)
                .WithSummary("Updates a teacher").Produces<TeacherDto>().ProducesValidationProblem().ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict).WithTags("Management");
            app.MapDelete("/management/teachers/{teacherId:int}", Deactivate).RequireAuthorization(AuthPolicies.SuperSecretary)
                .WithSummary("Soft deletes a teacher").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status404NotFound).WithTags("Management");
            app.MapPost("/management/teachers/{teacherId:int}/restore", Restore).RequireAuthorization(AuthPolicies.SuperSecretary)
                .WithSummary("Restores a teacher").Produces<TeacherDto>().ProducesProblem(StatusCodes.Status404NotFound).WithTags("Management");
        }

        private static async Task<Results<Ok<IReadOnlyCollection<TeacherDto>>, ProblemHttpResult>> GetAll(
            [FromQuery] int? specialtyId,
            [FromServices] Handler handler,
            CancellationToken ct)
        {
            var result = await handler.GetAllAsync(specialtyId, ct);
            return result.IsFailure ? result.ToProblemDetails() : TypedResults.Ok(result.Value!);
        }

        private static async Task<Results<Created<TeacherDto>, ProblemHttpResult, ValidationProblem>> Create(
            [FromBody] UpsertTeacherRequest request,
            [FromServices] IValidator<UpsertTeacherRequest> validator,
            [FromServices] Handler handler,
            CancellationToken ct)
        {
            ValidationResult validationResult = await validator.ValidateAsync(request, ct);
            if (!validationResult.IsValid)
            {
                return TypedResults.ValidationProblem(validationResult.ToDictionary());
            }

            var result = await handler.CreateAsync(request, ct);
            return result.IsFailure ? result.ToProblemDetails() : TypedResults.Created($"/api/management/teachers/{result.Value!.Id}", result.Value);
        }

        private static async Task<Results<Ok<TeacherDto>, ProblemHttpResult, ValidationProblem>> Update(
            [FromRoute] int teacherId,
            [FromBody] UpsertTeacherRequest request,
            [FromServices] IValidator<UpsertTeacherRequest> validator,
            [FromServices] Handler handler,
            CancellationToken ct)
        {
            ValidationResult validationResult = await validator.ValidateAsync(request, ct);
            if (!validationResult.IsValid)
            {
                return TypedResults.ValidationProblem(validationResult.ToDictionary());
            }

            var result = await handler.UpdateAsync(teacherId, request, ct);
            return result.IsFailure ? result.ToProblemDetails() : TypedResults.Ok(result.Value!);
        }

        private static async Task<Results<NoContent, ProblemHttpResult>> Deactivate([FromRoute] int teacherId, [FromServices] Handler handler, CancellationToken ct)
        {
            var result = await handler.SetActiveAsync(teacherId, isActive: false, ct);
            return result.IsFailure ? result.ToProblemDetails() : TypedResults.NoContent();
        }

        private static async Task<Results<Ok<TeacherDto>, ProblemHttpResult>> Restore([FromRoute] int teacherId, [FromServices] Handler handler, CancellationToken ct)
        {
            var result = await handler.SetActiveAsync(teacherId, isActive: true, ct);
            return result.IsFailure ? result.ToProblemDetails() : TypedResults.Ok(result.Value!);
        }
    }

    private sealed class Handler(DbDocGenContext context, SecretaryAccessService secretaryAccessService) : IScopedService
    {
        public async Task<Result<IReadOnlyCollection<TeacherDto>>> GetAllAsync(int? specialtyId, CancellationToken ct)
        {
            var guard = await secretaryAccessService.GetCurrentSuperSecretaryAsync(ct);
            if (guard.IsFailure)
            {
                return guard.ErrorDetails;
            }

            var query = context.Teachers.AsNoTracking();
            if (specialtyId is not null)
            {
                query = query.Where(t => t.SpecialtyId == specialtyId);
            }

            return await query
                .OrderBy(t => t.FullName)
                .Select(t => new TeacherDto(
                    t.Id,
                    t.FullName,
                    t.ShortName,
                    t.Email,
                    t.PhoneNumber,
                    t.AcademicDegreeId,
                    t.AcademicDegree!.ShortName,
                    t.TeacherPositionId,
                    t.TeacherPosition!.ShortName,
                    t.SpecialtyId,
                    t.Specialty!.Name,
                    t.IsActive))
                .ToListAsync(ct);
        }

        public async Task<Result<TeacherDto>> CreateAsync(UpsertTeacherRequest request, CancellationToken ct)
        {
            var guard = await secretaryAccessService.GetCurrentSuperSecretaryAsync(ct);
            if (guard.IsFailure)
            {
                return guard.ErrorDetails;
            }

            var validation = await ValidateDependenciesAsync(request, ct);
            if (validation.IsFailure)
            {
                return validation.ErrorDetails;
            }

            var email = request.Email.Trim();
            if (await context.Teachers.AnyAsync(t => t.Email == email, ct))
            {
                return ErrorDetails.Conflict("Teacher.AlreadyExists", "Teacher with the same email already exists.");
            }

            var teacher = new Teacher(
                request.FullName.Trim(),
                request.ShortName.Trim(),
                email,
                request.PhoneNumber.Trim(),
                request.AcademicDegreeId,
                request.TeacherPositionId,
                request.SpecialtyId)
            {
                IsActive = request.IsActive ?? true
            };

            await context.Teachers.AddAsync(teacher, ct);
            await context.SaveChangesAsync(ct);
            return await GetByIdAsync(teacher.Id, ct);
        }

        public async Task<Result<TeacherDto>> UpdateAsync(int teacherId, UpsertTeacherRequest request, CancellationToken ct)
        {
            var guard = await secretaryAccessService.GetCurrentSuperSecretaryAsync(ct);
            if (guard.IsFailure)
            {
                return guard.ErrorDetails;
            }

            var validation = await ValidateDependenciesAsync(request, ct);
            if (validation.IsFailure)
            {
                return validation.ErrorDetails;
            }

            var teacher = await context.Teachers.FirstOrDefaultAsync(t => t.Id == teacherId, ct);
            if (teacher is null)
            {
                return ErrorDetails.NotFound("Teacher.NotFound", "Teacher was not found.");
            }

            var email = request.Email.Trim();
            if (await context.Teachers.AnyAsync(t => t.Id != teacherId && t.Email == email, ct))
            {
                return ErrorDetails.Conflict("Teacher.AlreadyExists", "Teacher with the same email already exists.");
            }

            teacher.FullName = request.FullName.Trim();
            teacher.ShortName = request.ShortName.Trim();
            teacher.Email = email;
            teacher.PhoneNumber = request.PhoneNumber.Trim();
            teacher.AcademicDegreeId = request.AcademicDegreeId;
            teacher.TeacherPositionId = request.TeacherPositionId;
            teacher.SpecialtyId = request.SpecialtyId;
            teacher.IsActive = request.IsActive ?? teacher.IsActive;

            await context.SaveChangesAsync(ct);
            return await GetByIdAsync(teacher.Id, ct);
        }

        public async Task<Result<TeacherDto>> SetActiveAsync(int teacherId, bool isActive, CancellationToken ct)
        {
            var guard = await secretaryAccessService.GetCurrentSuperSecretaryAsync(ct);
            if (guard.IsFailure)
            {
                return guard.ErrorDetails;
            }

            var teacher = await context.Teachers.FirstOrDefaultAsync(t => t.Id == teacherId, ct);
            if (teacher is null)
            {
                return ErrorDetails.NotFound("Teacher.NotFound", "Teacher was not found.");
            }

            teacher.IsActive = isActive;
            await context.SaveChangesAsync(ct);
            return await GetByIdAsync(teacher.Id, ct);
        }

        private async Task<Result> ValidateDependenciesAsync(UpsertTeacherRequest request, CancellationToken ct)
        {
            if (!await context.Specialties.AnyAsync(s => s.Id == request.SpecialtyId && s.IsActive, ct))
            {
                return ErrorDetails.NotFound("Specialty.NotFound", "Active specialty was not found.");
            }

            if (!await context.AcademicDegrees.AnyAsync(d => d.Id == request.AcademicDegreeId && d.IsActive, ct))
            {
                return ErrorDetails.NotFound("AcademicDegree.NotFound", "Active academic degree was not found.");
            }

            if (!await context.TeacherPositions.AnyAsync(p => p.Id == request.TeacherPositionId && p.IsActive, ct))
            {
                return ErrorDetails.NotFound("TeacherPosition.NotFound", "Active teacher position was not found.");
            }

            return Result.Success();
        }

        private async Task<Result<TeacherDto>> GetByIdAsync(int teacherId, CancellationToken ct)
        {
            var teacher = await context.Teachers
                .AsNoTracking()
                .Where(t => t.Id == teacherId)
                .Select(t => new TeacherDto(
                    t.Id,
                    t.FullName,
                    t.ShortName,
                    t.Email,
                    t.PhoneNumber,
                    t.AcademicDegreeId,
                    t.AcademicDegree!.ShortName,
                    t.TeacherPositionId,
                    t.TeacherPosition!.ShortName,
                    t.SpecialtyId,
                    t.Specialty!.Name,
                    t.IsActive))
                .FirstOrDefaultAsync(ct);

            return teacher is null
                ? ErrorDetails.NotFound("Teacher.NotFound", "Teacher was not found.")
                : teacher;
        }
    }
}
