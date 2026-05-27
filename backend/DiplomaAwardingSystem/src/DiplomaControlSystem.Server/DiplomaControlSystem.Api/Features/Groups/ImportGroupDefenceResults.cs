using System.Globalization;
using Core.Api.Extensions;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using DiplomaControlSystem.Api.Infrastructure.Access;
using DiplomaControlSystem.Api.Infrastructure.DefenceResultImports;
using DiplomaControlSystem.Api.Infrastructure.Students;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DiplomaControlSystem.Api.Features.Groups;

public static class ImportGroupDefenceResults
{
    public sealed class ImportGroupDefenceResultsRequest
    {
        public IFormFile? ResultsFile { get; init; }

        [FromForm(Name = "googleDriveUrl")]
        public string? GoogleDriveLink { get; init; }
    }

    public sealed record ImportGroupDefenceResultsResponse(
        int GroupId,
        string GroupName,
        int RowsRead,
        int StudentsUpdated,
        int PlagiarismImported,
        int ScoresImported,
        int DefenceDatesImported);

    internal sealed class Validator : AbstractValidator<ImportGroupDefenceResultsRequest>
    {
        public Validator()
        {
            RuleFor(x => x)
                .Must(HaveExactlyOneResultSource)
                .WithMessage("Specify either results file or Google Drive URL, but not both.");

            RuleFor(x => x.ResultsFile)
                .Must(BeSupportedFile)
                .When(x => x.ResultsFile is not null)
                .WithMessage("Results file must be .xls, .xlsx, .xlsb, or .csv.");
        }

        private static bool HaveExactlyOneResultSource(ImportGroupDefenceResultsRequest request)
        {
            var hasFile = request.ResultsFile is not null;
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
            app.MapPost("/groups/{groupId:int}/defence-results/import", Handle)
                .DisableAntiforgery()
                .WithSummary("Imports defence results for all students in a group")
                .Produces<ImportGroupDefenceResultsResponse>(StatusCodes.Status200OK)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithTags("Groups");
        }

        private static async Task<Results<Ok<ImportGroupDefenceResultsResponse>, ProblemHttpResult, ValidationProblem>> Handle(
            [FromRoute] int groupId,
            [FromForm] ImportGroupDefenceResultsRequest request,
            [FromServices] IValidator<ImportGroupDefenceResultsRequest> validator,
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
        DefenceResultImportReader defenceResultImportReader) : IScopedService
    {
        public async Task<Result<ImportGroupDefenceResultsResponse>> HandleAsync(
            int groupId,
            ImportGroupDefenceResultsRequest request,
            CancellationToken ct)
        {
            var importResult = await defenceResultImportReader.ReadAsync(
                request.ResultsFile,
                request.GoogleDriveLink,
                ct);

            if (importResult.IsFailure)
            {
                return importResult.ErrorDetails;
            }

            var secretaryResult = await secretaryAccessService.GetCurrentSecretaryAsync(ct);
            if (secretaryResult.IsFailure)
            {
                return secretaryResult.ErrorDetails;
            }

            var group = await context.Groups
                .AsNoTracking()
                .Where(g => g.Id == groupId)
                .Select(g => new
                {
                    g.Id,
                    g.Name,
                    g.Year,
                    g.SpecialtyId
                })
                .FirstOrDefaultAsync(ct);

            if (group is null)
            {
                return ErrorDetails.NotFound(
                    "Group.NotFound",
                    "Group was not found.");
            }

            var secretary = secretaryResult.Value!;
            if (group.SpecialtyId != secretary.SpecialtyId)
            {
                return ErrorDetails.Forbidden(
                    "Group.Forbidden",
                    "Group does not belong to secretary specialty.");
            }

            if (!int.TryParse(group.Year, NumberStyles.None, CultureInfo.InvariantCulture, out var defenceYear))
            {
                return ErrorDetails.Validation(
                    "Group.DefenseYearInvalid",
                    "Group defense year must be a 4-digit year.");
            }

            var importedRows = importResult.Value!;
            var duplicateNames = FindNormalizedDuplicates(importedRows.Select(row => row.FullName));
            if (duplicateNames.Count > 0)
            {
                return ErrorDetails.Validation(
                    "DefenceResultImport.Duplicates",
                    string.Create(CultureInfo.InvariantCulture, $"Results table contains duplicate students: {string.Join(", ", duplicateNames)}."));
            }

            var importedRowsByName = importedRows.ToDictionary(
                row => NormalizeFullName(row.FullName),
                row => row);

            var students = await context.Students
                .Include(student => student.QualificationWork)
                .Where(student => student.GroupId == groupId)
                .ToListAsync(ct);

            var validationResult = ValidateStudentSet(students, importedRowsByName);
            if (validationResult.IsFailure)
            {
                return validationResult.ErrorDetails;
            }

            var parsedDatesResult = ParseDefenceDates(importedRows, defenceYear);
            if (parsedDatesResult.IsFailure)
            {
                return parsedDatesResult.ErrorDetails;
            }

            var parsedDatesByName = parsedDatesResult.Value!;
            var plagiarismImported = 0;
            var scoresImported = 0;
            var defenceDatesImported = 0;

            await using var transaction = await context.Database.BeginTransactionAsync(ct);

            foreach (var student in students)
            {
                var importedRow = importedRowsByName[NormalizeFullName(student.FullName)];
                var qualificationWork = StudentDiplomaDataInitializer.EnsureQualificationWork(student);

                qualificationWork.PlagiarismPercent = importedRow.PlagiarismPercent ?? 0;
                qualificationWork.UniquePercent = importedRow.PlagiarismPercent is null
                    ? 0
                    : 100 - importedRow.PlagiarismPercent.Value;
                qualificationWork.SupervisorScore = importedRow.SupervisorScore ?? 0;
                qualificationWork.ReviewerScore = importedRow.ReviewerScore ?? 0;
                qualificationWork.CommissionScore = importedRow.CommissionScore ?? 0;
                qualificationWork.EctsGrade = DefenceGradeCalculator.CalculateEctsGrade(qualificationWork.CommissionScore);
                qualificationWork.NationalGrade = DefenceGradeCalculator.CalculateNationalGrade(qualificationWork.CommissionScore);
                qualificationWork.DefenceDate = parsedDatesByName[NormalizeFullName(student.FullName)];

                if (importedRow.PlagiarismPercent is not null)
                {
                    plagiarismImported++;
                }

                if (importedRow.CommissionScore is not null
                    || importedRow.SupervisorScore is not null
                    || importedRow.ReviewerScore is not null)
                {
                    scoresImported++;
                }

                if (qualificationWork.DefenceDate is not null)
                {
                    defenceDatesImported++;
                }
            }

            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return new ImportGroupDefenceResultsResponse(
                group.Id,
                group.Name,
                importedRows.Count,
                students.Count,
                plagiarismImported,
                scoresImported,
                defenceDatesImported);
        }

        private static Result ValidateStudentSet(
            IReadOnlyCollection<Core.Domain.Entities.StudyGroup.Student> students,
            IReadOnlyDictionary<string, DefenceResultImportRow> importedRowsByName)
        {
            var studentsByName = students.ToDictionary(student => NormalizeFullName(student.FullName));
            var missingInDatabase = importedRowsByName.Keys
                .Where(name => !studentsByName.ContainsKey(name))
                .ToList();

            if (missingInDatabase.Count > 0)
            {
                return ErrorDetails.Validation(
                    "DefenceResultImport.StudentsNotFound",
                    string.Create(CultureInfo.InvariantCulture, $"Students from table were not found in group: {string.Join(", ", missingInDatabase)}."));
            }

            var missingInTable = studentsByName.Keys
                .Where(name => !importedRowsByName.ContainsKey(name))
                .ToList();

            if (missingInTable.Count > 0)
            {
                return ErrorDetails.Validation(
                    "DefenceResultImport.GroupStudentsMissing",
                    string.Create(CultureInfo.InvariantCulture, $"Group students are missing from results table: {string.Join(", ", missingInTable)}."));
            }

            return Result.Success();
        }

        private static Result<Dictionary<string, DateOnly?>> ParseDefenceDates(
            IReadOnlyCollection<DefenceResultImportRow> importedRows,
            int defenceYear)
        {
            var datesByName = new Dictionary<string, DateOnly?>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in importedRows)
            {
                var parseResult = ParseDefenceDate(row.DefenceDate, defenceYear);
                if (parseResult.IsFailure)
                {
                    return ErrorDetails.Validation(
                        "DefenceResultImport.DefenceDateInvalid",
                        string.Create(CultureInfo.InvariantCulture, $"Defence date for student {row.FullName} is invalid."));
                }

                datesByName.Add(NormalizeFullName(row.FullName), parseResult.Value);
            }

            return datesByName;
        }

        private static Result<DateOnly?> ParseDefenceDate(string value, int defenceYear)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return (DateOnly?)null;
            }

            var trimmed = value.Trim();
            var formats = new[] { "d.M", "dd.MM", "d.M.", "dd.MM.", "d/M", "dd/MM" };
            if (DateTime.TryParseExact(
                    trimmed,
                    formats,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsedDayMonth))
            {
                return CreateDate(parsedDayMonth.Day, parsedDayMonth.Month, defenceYear);
            }

            var normalized = trimmed.Replace(',', '.');
            if (double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var serialDate)
                && serialDate > 31)
            {
                var date = DateTime.FromOADate(serialDate);
                return CreateDate(date.Day, date.Month, defenceYear);
            }

            return ErrorDetails.Validation(
                "DefenceResultImport.DefenceDateInvalid",
                "Defence date must be in day.month format.");
        }

        private static Result<DateOnly?> CreateDate(int day, int month, int year)
        {
            try
            {
                return new DateOnly(year, month, day);
            }
            catch (ArgumentOutOfRangeException)
            {
                return ErrorDetails.Validation(
                    "DefenceResultImport.DefenceDateInvalid",
                    "Defence date is invalid.");
            }
        }

        private static string NormalizeFullName(string value)
        {
            return string.Join(
                    ' ',
                    value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .ToUpperInvariant();
        }

        private static List<string> FindNormalizedDuplicates(IEnumerable<string> names)
        {
            var uniqueNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var duplicates = new List<string>();

            foreach (var name in names)
            {
                var normalizedName = NormalizeFullName(name);
                if (!uniqueNames.Add(normalizedName))
                {
                    duplicates.Add(normalizedName);
                }
            }

            return duplicates;
        }
    }
}
