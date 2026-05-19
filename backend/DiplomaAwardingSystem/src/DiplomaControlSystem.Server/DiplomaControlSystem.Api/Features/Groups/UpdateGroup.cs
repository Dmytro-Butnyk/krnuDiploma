using System.Globalization;
using System.Text.RegularExpressions;
using Core.Api.Extensions;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.Enums;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using DiplomaControlSystem.Api.Infrastructure.Access;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DiplomaControlSystem.Api.Features.Groups;

public static partial class UpdateGroup
{
    public sealed record Request(
        string SecretaryEmail,
        string? Name,
        string? Year,
        string? EducationLevel);

    public sealed record Response(
        int Id,
        string Name,
        string Year,
        string EducationLevel);

    private static bool TryParseEducationLevel(string? educationLevel, out EducationLevel parsedEducationLevel)
    {
        parsedEducationLevel = EducationLevel.None;
        return string.IsNullOrWhiteSpace(educationLevel)
               || (Enum.TryParse(educationLevel, ignoreCase: true, out parsedEducationLevel)
                   && parsedEducationLevel != EducationLevel.None);
    }

    private static bool TryNormalizeStartYear(string? year, out string? normalizedYear)
    {
        normalizedYear = null;
        if (string.IsNullOrWhiteSpace(year))
        {
            return true;
        }

        var trimmedYear = year.Trim();
        var separatorIndex = trimmedYear.IndexOf('/', StringComparison.Ordinal);
        var firstPart = separatorIndex >= 0
            ? trimmedYear[..separatorIndex].Trim()
            : trimmedYear;

        if (!StartYearRegex().IsMatch(firstPart))
        {
            return false;
        }

        normalizedYear = firstPart;
        return true;
    }

    private static string FormatAcademicYear(string year)
    {
        return int.TryParse(year, NumberStyles.Integer, CultureInfo.InvariantCulture, out var startYear)
            ? string.Create(CultureInfo.InvariantCulture, $"{startYear}/{(startYear + 1) % 100:00}")
            : year;
    }

    internal sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.SecretaryEmail)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(320);

            RuleFor(x => x.Name)
                .MaximumLength(100)
                .When(x => x.Name is not null);

            RuleFor(x => x.Year)
                .Must(year => TryNormalizeStartYear(year, out _))
                .WithMessage("Year must be a start year like 2025 or academic year like 2025/26.")
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
                .Produces<Response>(StatusCodes.Status200OK)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status409Conflict)
                .WithTags("Groups");
        }

        private static async Task<Results<Ok<Response>, ProblemHttpResult, ValidationProblem>> Handle(
            [FromRoute] int groupId,
            [FromBody] Request request,
            [FromServices] IValidator<Request> validator,
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
        SecretaryAccessService secretaryAccessService) : IScopedService
    {
        public async Task<Result<Response>> HandleAsync(
            int groupId,
            Request request,
            CancellationToken ct)
        {
            var secretaryResult = await secretaryAccessService.GetActiveSecretaryAsync(request.SecretaryEmail, ct);
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

            _ = TryNormalizeStartYear(request.Year, out var normalizedYear);
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

            await context.SaveChangesAsync(ct);

            return new Response(
                group.Id,
                group.Name,
                FormatAcademicYear(group.Year),
                group.EducationLevel.ToString());
        }
    }

    [GeneratedRegex(@"^\d{4}$")]
    private static partial Regex StartYearRegex();
}
