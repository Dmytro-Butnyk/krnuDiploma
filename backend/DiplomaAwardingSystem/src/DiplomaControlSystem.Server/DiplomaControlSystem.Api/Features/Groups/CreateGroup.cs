using System.Text.RegularExpressions;
using Core.Api.Extensions;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.Enums;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using DiplomaControlSystem.Api.Infrastructure.Access;
using DiplomaControlSystem.Api.Infrastructure.AcademicYears;
using DiplomaControlSystem.Api.Infrastructure.Students;
using DiplomaControlSystem.Api.Infrastructure.StudentImports;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DomainGroup = Core.Domain.Entities.StudyGroup.Group;

namespace DiplomaControlSystem.Api.Features.Groups;

public static partial class CreateGroup
{
    public sealed class CreateGroupRequest
    {
        public string Name { get; init; } = string.Empty;
        public string Year { get; init; } = string.Empty;
        public string EducationLevel { get; init; } = string.Empty;
        public IFormFile? StudentsFile { get; init; }

        [FromForm(Name = "googleDriveUrl")]
        public string? GoogleDriveLink { get; init; }
    }

    public sealed record CreateGroupResponse(
        int GroupId,
        string Name,
        string Year,
        string DefenseYear,
        string EducationLevel,
        int StudentsCreated,
        StudentImportStatisticsDto ImportStatistics);

    public sealed record StudentImportStatisticsDto(
        int SupervisorsMatched,
        int SupervisorsMissing,
        int SupervisorsUnspecified,
        int TopicsImported,
        int PracticeBasesImported);

    private static bool TryParseEducationLevel(string educationLevel, out EducationLevel parsedEducationLevel)
    {
        return Enum.TryParse(educationLevel, ignoreCase: true, out parsedEducationLevel)
               && parsedEducationLevel != EducationLevel.None;
    }

    internal sealed class Validator : AbstractValidator<CreateGroupRequest>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.Year)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .Must(year => AcademicYearRules.TryNormalizeDefenseYear(year, out _))
                .WithMessage("Year must be a 4-digit defense year like 2026.")
                .Must(AcademicYearRules.IsAllowedDefenseYear)
                .WithMessage(_ => AcademicYearRules.GetAllowedDefenseYearRangeMessage());

            RuleFor(x => x.EducationLevel)
                .NotEmpty()
                .Must(level => TryParseEducationLevel(level, out _))
                .WithMessage("Education level must be Bachelor or Master.");

            RuleFor(x => x)
                .Must(HaveExactlyOneStudentSource)
                .WithMessage("Specify either students file or Google Drive URL, but not both.");

            RuleFor(x => x.StudentsFile)
                .Must(BeSupportedFile)
                .When(x => x.StudentsFile is not null)
                .WithMessage("Students file must be .xls, .xlsx, .xlsb, or .csv.");
        }

        private static bool HaveExactlyOneStudentSource(CreateGroupRequest request)
        {
            var hasFile = request.StudentsFile is not null;
            var hasGoogleDriveLink = !string.IsNullOrWhiteSpace(request.GoogleDriveLink);
            return hasFile != hasGoogleDriveLink;
        }

        private static bool BeSupportedFile(IFormFile? file)
        {
            if (file is null)
            {
                return true;
            }

            var extension = Path.GetExtension(file.FileName);
            return extension.Equals(".xls", StringComparison.OrdinalIgnoreCase)
                   || extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase)
                   || extension.Equals(".xlsb", StringComparison.OrdinalIgnoreCase)
                   || extension.Equals(".csv", StringComparison.OrdinalIgnoreCase);
        }
    }

    internal static class Endpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/groups", Handle)
                .DisableAntiforgery()
                .WithSummary("Creates a group with imported students and default diploma data")
                .Produces<CreateGroupResponse>(StatusCodes.Status201Created)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status409Conflict)
                .WithTags("Groups");
        }

        private static async Task<Results<Created<CreateGroupResponse>, ProblemHttpResult, ValidationProblem>> Handle(
            [FromForm] CreateGroupRequest request,
            [FromServices] IValidator<CreateGroupRequest> validator,
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

            return TypedResults.Created($"/api/groups/{result.Value!.GroupId}", result.Value);
        }
    }

    private sealed partial class Handler(
        DbDocGenContext context,
        SecretaryAccessService secretaryAccessService,
        StudentImportReader studentImportReader) : IScopedService
    {
        public async Task<Result<CreateGroupResponse>> HandleAsync(CreateGroupRequest request, CancellationToken ct)
        {
            _ = TryParseEducationLevel(request.EducationLevel, out var educationLevel);
            _ = AcademicYearRules.TryNormalizeDefenseYear(request.Year, out var normalizedYear);

            var studentsImportResult = await studentImportReader.ReadAsync(
                request.StudentsFile,
                request.GoogleDriveLink,
                ct);

            if (studentsImportResult.IsFailure)
            {
                return studentsImportResult.ErrorDetails;
            }

            var secretaryResult = await secretaryAccessService.GetCurrentSecretaryAsync(ct);
            if (secretaryResult.IsFailure)
            {
                return secretaryResult.ErrorDetails;
            }

            var secretary = secretaryResult.Value!;
            var groupName = request.Name.Trim();
            var groupExists = await context.Groups
                .AnyAsync(
                    group => group.SpecialtyId == secretary.SpecialtyId
                             && group.Name == groupName
                             && group.Year == normalizedYear
                             && group.EducationLevel == educationLevel,
                    ct);

            if (groupExists)
            {
                return ErrorDetails.Conflict(
                    "Group.AlreadyExists",
                    "Group with the same name, year, and education level already exists.");
            }

            await using var transaction = await context.Database.BeginTransactionAsync(ct);

            var group = new DomainGroup(groupName, normalizedYear, educationLevel, secretary.SpecialtyId);
            group.DiplomaExaminationCommissionId = await context.DiplomaExaminationCommissions
                .AsNoTracking()
                .Where(dec => dec.DefenseYear == normalizedYear)
                .Where(dec => dec.SpecialtyId == secretary.SpecialtyId)
                .Where(dec => dec.EducationLevel == educationLevel)
                .Select(dec => (int?)dec.Id)
                .FirstOrDefaultAsync(ct);

            var importedStudents = studentsImportResult.Value!;
            var studentsCount = importedStudents.Count;
            var supervisorIdsByShortName = await GetUniqueTeacherIdsByShortNameAsync(secretary.SpecialtyId, ct);
            var supervisorsMatched = 0;
            var supervisorsMissing = 0;
            var supervisorsUnspecified = 0;
            var topicsImported = 0;
            var practiceBasesImported = 0;

            foreach (var importedStudent in importedStudents)
            {
                int? supervisorId = null;
                if (string.IsNullOrWhiteSpace(importedStudent.SupervisorShortName))
                {
                    supervisorsUnspecified++;
                }
                else if (supervisorIdsByShortName.TryGetValue(
                             NormalizeTeacherShortName(importedStudent.SupervisorShortName),
                             out var matchedSupervisorId))
                {
                    supervisorId = matchedSupervisorId;
                    supervisorsMatched++;
                }
                else
                {
                    supervisorsMissing++;
                }

                if (!string.IsNullOrWhiteSpace(importedStudent.Topic))
                {
                    topicsImported++;
                }

                if (!string.IsNullOrWhiteSpace(importedStudent.PracticeBase))
                {
                    practiceBasesImported++;
                }

                group.Students.Add(StudentDraftFactory.Create(
                    importedStudent.FullName,
                    importedStudent.Topic,
                    importedStudent.PracticeBase,
                    supervisorId));
            }

            await context.Groups.AddAsync(group, ct);
            await context.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);

            return new CreateGroupResponse(
                group.Id,
                group.Name,
                AcademicYearRules.FormatAcademicYearFromDefenseYear(group.Year),
                group.Year,
                group.EducationLevel.ToString(),
                studentsCount,
                new StudentImportStatisticsDto(
                    supervisorsMatched,
                    supervisorsMissing,
                    supervisorsUnspecified,
                    topicsImported,
                    practiceBasesImported));
        }

        private async Task<Dictionary<string, int>> GetUniqueTeacherIdsByShortNameAsync(
            int specialtyId,
            CancellationToken ct)
        {
            var teachers = await context.Teachers
                .AsNoTracking()
                .Where(t => t.IsActive)
                .Where(t => t.SpecialtyId == specialtyId)
                .Select(t => new { t.Id, t.ShortName })
                .ToListAsync(ct);

            return teachers
                .Where(t => !string.IsNullOrWhiteSpace(t.ShortName))
                .GroupBy(t => NormalizeTeacherShortName(t.ShortName))
                .Where(group => group.Count() == 1)
                .ToDictionary(group => group.Key, group => group.Single().Id);
        }

        private static string NormalizeTeacherShortName(string value)
        {
            var normalized = WhitespaceRegex().Replace(value.Trim(), " ");
            return InitialDotSpacingRegex().Replace(normalized, ".").ToUpperInvariant();
        }

        [GeneratedRegex(@"\s+")]
        private static partial Regex WhitespaceRegex();

        [GeneratedRegex(@"\s*\.\s*")]
        private static partial Regex InitialDotSpacingRegex();
    }
}
