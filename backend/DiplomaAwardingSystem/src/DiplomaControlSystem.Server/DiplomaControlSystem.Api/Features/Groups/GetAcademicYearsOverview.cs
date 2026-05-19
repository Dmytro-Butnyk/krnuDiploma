using Core.Api.Extensions;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.Enums;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using DiplomaControlSystem.Api.Infrastructure.Groups;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DiplomaControlSystem.Api.Features.Groups;

public static class GetAcademicYearsOverview
{
    public sealed class Request
    {
        public string SecretaryEmail { get; init; } = string.Empty;
        public string EducationLevel { get; init; } = string.Empty;
    }

    public sealed record GroupDto(int Id, string Name);

    public sealed record AcademicYearOverviewDto(
        string Year,
        string DefenseYear,
        IReadOnlyCollection<GroupDto> Groups);

    private static bool TryParseEducationLevel(string educationLevel, out EducationLevel parsedEducationLevel)
    {
        return Enum.TryParse(educationLevel, ignoreCase: true, out parsedEducationLevel)
               && parsedEducationLevel != EducationLevel.None;
    }

    internal sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.SecretaryEmail)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(320);

            RuleFor(x => x.EducationLevel)
                .NotEmpty()
                .Must(BeValidEducationLevel)
                .WithMessage("Education level must be Bachelor or Master.");
        }

        private static bool BeValidEducationLevel(string educationLevel)
        {
            return TryParseEducationLevel(educationLevel, out _);
        }
    }

    internal static class Endpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/groups/academic-years", Handle)
                .WithSummary("Gets academic years and groups available to a secretary")
                .Produces<IReadOnlyCollection<AcademicYearOverviewDto>>(StatusCodes.Status200OK)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithTags("Groups");
        }

        private static async Task<Results<Ok<IReadOnlyCollection<AcademicYearOverviewDto>>, ProblemHttpResult, ValidationProblem>> Handle(
            [AsParameters] Request request,
            [FromServices] IValidator<Request> validator,
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

            return TypedResults.Ok(result.Value!);
        }
    }

    private sealed class Handler(DbDocGenContext context) : IScopedService
    {
        public async Task<Result<IReadOnlyCollection<AcademicYearOverviewDto>>> HandleAsync(
            Request request,
            CancellationToken ct)
        {
            var email = request.SecretaryEmail.Trim();
            _ = TryParseEducationLevel(request.EducationLevel, out var educationLevel);

            var secretary = await context.Secretaries
                .AsNoTracking()
                .Where(s => EF.Functions.ILike(s.Email, email))
                .Select(s => new
                {
                    s.SpecialtyId,
                    s.IsActive
                })
                .FirstOrDefaultAsync(ct);

            if (secretary is null)
            {
                return ErrorDetails.NotFound(
                    "Secretary.NotFound",
                    "Secretary with the specified email was not found.");
            }

            if (!secretary.IsActive)
            {
                return ErrorDetails.Forbidden(
                    "Secretary.Inactive",
                    "Secretary with the specified email is inactive.");
            }

            var groups = await context.Groups
                .AsNoTracking()
                .Where(g => g.SpecialtyId == secretary.SpecialtyId)
                .Where(g => g.EducationLevel == educationLevel)
                .Select(g => new GroupProjection(g.Id, g.Name, g.Year))
                .ToListAsync(ct);

            return groups
                .GroupBy(g => g.Year, StringComparer.Ordinal)
                .OrderByDescending(g => GroupYearRules.GetDefenseYearSortKey(g.Key) ?? int.MinValue)
                .Select(g => new AcademicYearOverviewDto(
                    GroupYearRules.FormatAcademicYearFromDefenseYear(g.Key),
                    g.Key,
                    g.OrderBy(group => group.Name, StringComparer.Ordinal)
                        .Select(group => new GroupDto(group.Id, group.Name))
                        .ToList()))
                .ToList();
        }

        private sealed record GroupProjection(int Id, string Name, string Year);
    }
}
