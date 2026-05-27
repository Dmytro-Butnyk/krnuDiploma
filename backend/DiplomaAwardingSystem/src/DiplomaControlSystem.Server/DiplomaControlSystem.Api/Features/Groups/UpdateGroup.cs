using Core.Api.Extensions;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.Enums;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using DiplomaControlSystem.Api.Infrastructure.Access;
using DiplomaControlSystem.Api.Infrastructure.AcademicYears;
using DiplomaControlSystem.Api.Infrastructure.DiplomaExaminationCommissions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DiplomaControlSystem.Api.Features.Groups;

public static class UpdateGroup
{
    public sealed record UpdateGroupRequest(
        string? Name,
        string? Year,
        string? EducationLevel);

    public sealed record UpdateGroupResponse(
        int Id,
        string Name,
        string Year,
        string DefenseYear,
        string EducationLevel);

    private static bool TryParseEducationLevel(string? educationLevel, out EducationLevel parsedEducationLevel)
    {
        parsedEducationLevel = EducationLevel.None;
        return string.IsNullOrWhiteSpace(educationLevel)
               || (Enum.TryParse(educationLevel, ignoreCase: true, out parsedEducationLevel)
                   && parsedEducationLevel != EducationLevel.None);
    }

    internal sealed class Validator : AbstractValidator<UpdateGroupRequest>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .MaximumLength(100)
                .When(x => x.Name is not null);

            RuleFor(x => x.Year)
                .Cascade(CascadeMode.Stop)
                .Must(year => AcademicYearRules.TryNormalizeDefenseYear(year, out _))
                .WithMessage("Year must be a 4-digit defense year like 2026.")
                .Must(AcademicYearRules.IsAllowedDefenseYear)
                .WithMessage(_ => AcademicYearRules.GetAllowedDefenseYearRangeMessage())
                .When(x => x.Year is not null);

            RuleFor(x => x.EducationLevel)
                .Must(level => TryParseEducationLevel(level, out _))
                .WithMessage("Education level must be Bachelor or Master.")
                .When(x => x.EducationLevel is not null);
        }
    }

    internal static class Endpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPatch("/groups/{groupId:int}", Handle)
                .WithSummary("Updates group general information")
                .Produces<UpdateGroupResponse>(StatusCodes.Status200OK)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status409Conflict)
                .WithTags("Groups");
        }

        private static async Task<Results<Ok<UpdateGroupResponse>, ProblemHttpResult, ValidationProblem>> Handle(
            [FromRoute] int groupId,
            [FromBody] UpdateGroupRequest request,
            [FromServices] IValidator<UpdateGroupRequest> validator,
            [FromServices] Handler handler,
            CancellationToken ct)
        {
            ValidationResult validationResult = await validator.ValidateAsync(request, ct);

            if (!validationResult.IsValid)
            {
                return TypedResults.ValidationProblem(validationResult.ToDictionary());
            }

            var result = await handler.HandleAsync(groupId, request, ct);

            if (result.IsFailure)
            {
                return result.ToProblemDetails();
            }

            return TypedResults.Ok(result.Value!);
        }
    }

    private sealed class Handler(
        DbDocGenContext context,
        SecretaryAccessService secretaryAccessService,
        DiplomaExaminationCommissionCleanupService commissionCleanupService) : IScopedService
    {
        public async Task<Result<UpdateGroupResponse>> HandleAsync(
            int groupId,
            UpdateGroupRequest request,
            CancellationToken ct)
        {
            var secretaryResult = await secretaryAccessService.GetCurrentSecretaryAsync(ct);
            if (secretaryResult.IsFailure)
            {
                return secretaryResult.ErrorDetails;
            }

            var secretary = secretaryResult.Value!;
            var group = await context.Groups.FirstOrDefaultAsync(g => g.Id == groupId, ct);

            if (group is null)
            {
                return ErrorDetails.NotFound(
                    "Group.NotFound",
                    "Group was not found.");
            }

            if (group.SpecialtyId != secretary.SpecialtyId)
            {
                return ErrorDetails.Forbidden(
                    "Group.Forbidden",
                    "Group does not belong to secretary specialty.");
            }

            string? normalizedYear = null;
            if (request.Year is not null)
            {
                _ = AcademicYearRules.TryNormalizeDefenseYear(request.Year, out normalizedYear);
            }

            _ = TryParseEducationLevel(request.EducationLevel, out var parsedEducationLevel);

            var nextName = string.IsNullOrWhiteSpace(request.Name) ? group.Name : request.Name.Trim();
            var nextYear = normalizedYear ?? group.Year;
            var nextEducationLevel = parsedEducationLevel == EducationLevel.None ? group.EducationLevel : parsedEducationLevel;

            var duplicateExists = await context.Groups
                .AnyAsync(
                    existingGroup => existingGroup.Id != group.Id
                                     && existingGroup.SpecialtyId == group.SpecialtyId
                                     && existingGroup.Name == nextName
                                     && existingGroup.Year == nextYear
                                     && existingGroup.EducationLevel == nextEducationLevel,
                    ct);

            if (duplicateExists)
            {
                return ErrorDetails.Conflict(
                    "Group.AlreadyExists",
                    "Group with the same name, year, and education level already exists.");
            }

            request.Name.UpdateIfNotNull(value => group.Name = value.Trim());
            normalizedYear.UpdateIfNotNull(value => group.Year = value);
            request.EducationLevel.UpdateIfNotNull(_ => group.EducationLevel = nextEducationLevel);
            var previousCommissionId = group.DiplomaExaminationCommissionId;
            group.DiplomaExaminationCommissionId = await context.DiplomaExaminationCommissions
                .AsNoTracking()
                .Where(dec => dec.DefenseYear == group.Year)
                .Where(dec => dec.SpecialtyId == group.SpecialtyId)
                .Where(dec => dec.EducationLevel == group.EducationLevel)
                .Select(dec => (int?)dec.Id)
                .FirstOrDefaultAsync(ct);

            await using var transaction = await context.Database.BeginTransactionAsync(ct);

            await context.SaveChangesAsync(ct);

            if (previousCommissionId != group.DiplomaExaminationCommissionId)
            {
                await commissionCleanupService.RemoveEmptyCommissionsAsync([previousCommissionId], ct);
                await context.SaveChangesAsync(ct);
            }

            await transaction.CommitAsync(ct);

            return new UpdateGroupResponse(
                group.Id,
                group.Name,
                AcademicYearRules.FormatAcademicYearFromDefenseYear(group.Year),
                group.Year,
                group.EducationLevel.ToString());
        }
    }
}
