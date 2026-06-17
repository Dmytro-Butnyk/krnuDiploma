using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.ResultPattern;
using ExcelDataReader;

namespace DiplomaControlSystem.Api.Infrastructure.StudentImports;

internal sealed partial class StudentImportReader(IHttpClientFactory httpClientFactory) : IScopedService
{
    private const int MaxStudentsCount = 500;
    private const int TopicMaxLength = 500;
    private const int PracticeBaseMaxLength = 256;

    static StudentImportReader()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public async Task<Result<IReadOnlyCollection<StudentImportRow>>> ReadAsync(
        IFormFile? studentsFile,
        string? googleDriveUrl,
        CancellationToken ct)
    {
        if (studentsFile is not null)
        {
            await using var stream = studentsFile.OpenReadStream();
            return ReadFromStream(stream, studentsFile.FileName);
        }

        if (!string.IsNullOrWhiteSpace(googleDriveUrl))
        {
            return await ReadFromGoogleUrlAsync(googleDriveUrl, ct);
        }

        return ErrorDetails.Validation(
            "StudentImport.SourceMissing",
            "Students file or Google Sheets URL is required.");
    }

    private async Task<Result<IReadOnlyCollection<StudentImportRow>>> ReadFromGoogleUrlAsync(
        string googleDriveUrl,
        CancellationToken ct)
    {
        var exportUrlResult = BuildGoogleDownloadUrl(googleDriveUrl);
        if (exportUrlResult.IsFailure)
        {
            return exportUrlResult.ErrorDetails;
        }

        using var httpClient = httpClientFactory.CreateClient();
        using var response = await httpClient.GetAsync(exportUrlResult.Value!.Url, ct);
        if (!response.IsSuccessStatusCode)
        {
            return ErrorDetails.Validation(
                "StudentImport.GoogleSourceUnavailable",
                "Google table is unavailable. Check that the link is public.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        return ReadFromStream(stream, exportUrlResult.Value.FileName);
    }

    private static Result<GoogleDownloadSource> BuildGoogleDownloadUrl(string source)
    {
        var trimmedSource = source.Trim();
        var sheetsMatch = GoogleSheetIdRegex().Match(trimmedSource);
        if (sheetsMatch.Success)
        {
            var gidMatch = GoogleSheetGidRegex().Match(trimmedSource);
            return new GoogleDownloadSource(
                BuildGoogleSheetsCsvExportUri(
                    sheetsMatch.Groups["id"].Value,
                    gidMatch.Success ? gidMatch.Groups["gid"].Value : null),
                "students.csv");
        }

        var driveMatch = GoogleDriveFileIdRegex().Match(trimmedSource);
        if (driveMatch.Success)
        {
            return new GoogleDownloadSource(
                BuildGoogleDriveDownloadUri(driveMatch.Groups["id"].Value),
                "students.xlsx");
        }

        if (GoogleBareIdRegex().IsMatch(trimmedSource))
        {
            return new GoogleDownloadSource(
                BuildGoogleSheetsCsvExportUri(trimmedSource, gid: null),
                "students.csv");
        }

        return ErrorDetails.Validation(
            "StudentImport.UnsupportedGoogleUrl",
            "Google URL must be a public Google Sheets link or Google Drive file link.");
    }

    private static Result<IReadOnlyCollection<StudentImportRow>> ReadFromStream(Stream stream, string fileName)
    {
        using var reader = CreateReader(stream, fileName);
        var rowsResult = ReadStudentRows(reader);
        if (rowsResult.IsFailure)
        {
            return rowsResult.ErrorDetails;
        }

        var rows = rowsResult.Value!;
        if (rows.Count == 0)
        {
            return ErrorDetails.Validation(
                "StudentImport.Empty",
                "Students table does not contain any names.");
        }

        if (rows.Count > MaxStudentsCount)
        {
            return ErrorDetails.Validation(
                "StudentImport.TooManyStudents",
                string.Create(CultureInfo.InvariantCulture, $"Students count cannot exceed {MaxStudentsCount}."));
        }

        var duplicates = FindDuplicates(rows.Select(row => row.FullName));
        if (duplicates.Count > 0)
        {
            return ErrorDetails.Validation(
                "StudentImport.Duplicates",
                string.Create(CultureInfo.InvariantCulture, $"Students table contains duplicate names: {string.Join(", ", duplicates)}."));
        }

        var tooLongTopic = rows.FirstOrDefault(row => row.Topic.Length > TopicMaxLength);
        if (tooLongTopic is not null)
        {
            return ErrorDetails.Validation(
                "StudentImport.TopicTooLong",
                string.Create(CultureInfo.InvariantCulture, $"Topic for student {tooLongTopic.FullName} cannot exceed {TopicMaxLength} characters."));
        }

        var tooLongPracticeBase = rows.FirstOrDefault(row => row.PracticeBase.Length > PracticeBaseMaxLength);
        if (tooLongPracticeBase is not null)
        {
            return ErrorDetails.Validation(
                "StudentImport.PracticeBaseTooLong",
                string.Create(CultureInfo.InvariantCulture, $"Practice base for student {tooLongPracticeBase.FullName} cannot exceed {PracticeBaseMaxLength} characters."));
        }

        return rows;
    }

    private static IExcelDataReader CreateReader(Stream stream, string fileName)
    {
        var extension = Path.GetExtension(fileName);

        if (extension.Equals(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return ExcelReaderFactory.CreateCsvReader(stream);
        }

        return ExcelReaderFactory.CreateReader(stream);
    }

    private static Result<List<StudentImportRow>> ReadStudentRows(IExcelDataReader reader)
    {
        var students = new List<StudentImportRow>();
        StudentImportColumns? columns = null;

        while (reader.Read())
        {
            if (columns is null)
            {
                columns = TryDetectColumns(reader);
                continue;
            }

            var fullName = NormalizeCellValue(reader.GetValue(columns.StudentFullNameColumnIndex));
            if (!string.IsNullOrWhiteSpace(fullName))
            {
                students.Add(new StudentImportRow(
                    fullName,
                    GetOptionalCellValue(reader, columns.SupervisorColumnIndex),
                    GetOptionalCellValue(reader, columns.TopicColumnIndex),
                    GetOptionalCellValue(reader, columns.PracticeBaseColumnIndex)));
            }
        }

        if (columns is null)
        {
            return ErrorDetails.Validation(
                "StudentImport.StudentNameHeaderMissing",
                "Students table must contain a student full name column.");
        }

        return students;
    }

    private static StudentImportColumns? TryDetectColumns(IExcelDataReader reader)
    {
        int? studentFullNameColumnIndex = null;
        int? supervisorColumnIndex = null;
        int? topicColumnIndex = null;
        int? practiceBaseColumnIndex = null;

        for (var i = 0; i < reader.FieldCount; i++)
        {
            var value = NormalizeHeaderValue(reader.GetValue(i));
            if (StudentImportColumnDefinitions.StudentFullNameHeaderNames.Contains(value))
            {
                studentFullNameColumnIndex = i;
            }
            else if (IsSupervisorNameHeader(value))
            {
                supervisorColumnIndex = i;
            }
            else if (StudentImportColumnDefinitions.TopicHeaderNames.Contains(value))
            {
                topicColumnIndex = i;
            }
            else if (StudentImportColumnDefinitions.PracticeBaseHeaderNames.Contains(value))
            {
                practiceBaseColumnIndex = i;
            }
        }

        return studentFullNameColumnIndex is null
            ? null
            : new StudentImportColumns(
                studentFullNameColumnIndex.Value,
                supervisorColumnIndex,
                topicColumnIndex,
                practiceBaseColumnIndex);
    }

    private static string GetOptionalCellValue(IExcelDataReader reader, int? columnIndex)
    {
        return columnIndex is null ? string.Empty : NormalizeCellValue(reader.GetValue(columnIndex.Value));
    }

    private static string NormalizeCellValue(object? value)
    {
        var text = Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
        return WhitespaceRegex().Replace(text, " ");
    }

    private static string NormalizeHeaderValue(object? value)
    {
        var normalized = NormalizeCellValue(value)
            .Replace(".", string.Empty, StringComparison.Ordinal)
            .ToUpperInvariant();

        return WhitespaceRegex().Replace(normalized, " ");
    }

    private static bool IsSupervisorNameHeader(string value)
    {
        if (StudentImportColumnDefinitions.SupervisorHeaderNames.Contains(value))
        {
            return true;
        }

        return value.Contains("\u041a\u0415\u0420\u0406\u0412\u041d", StringComparison.Ordinal)
               && !value.Contains("\u041e\u0426\u0406\u041d", StringComparison.Ordinal);
    }

    private static List<string> FindDuplicates(IEnumerable<string> names)
    {
        var uniqueNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var duplicates = new List<string>();

        foreach (var name in names)
        {
            if (!uniqueNames.Add(name))
            {
                duplicates.Add(name);
            }
        }

        return duplicates;
    }

    private static Uri BuildGoogleSheetsCsvExportUri(string sheetId, string? gid)
    {
        var builder = new UriBuilder
        {
            Scheme = Uri.UriSchemeHttps,
            Host = "docs.google.com",
            Path = string.Create(CultureInfo.InvariantCulture, $"spreadsheets/d/{sheetId}/export"),
            Query = string.IsNullOrWhiteSpace(gid)
                ? "format=csv"
                : string.Create(CultureInfo.InvariantCulture, $"format=csv&gid={gid}")
        };

        return builder.Uri;
    }

    private static Uri BuildGoogleDriveDownloadUri(string fileId)
    {
        var builder = new UriBuilder
        {
            Scheme = Uri.UriSchemeHttps,
            Host = "drive.google.com",
            Path = "uc",
            Query = string.Create(CultureInfo.InvariantCulture, $"export=download&id={fileId}")
        };

        return builder.Uri;
    }

    private sealed record StudentImportColumns(
        int StudentFullNameColumnIndex,
        int? SupervisorColumnIndex,
        int? TopicColumnIndex,
        int? PracticeBaseColumnIndex);

    private sealed record GoogleDownloadSource(Uri Url, string FileName);

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"docs\.google\.com/spreadsheets/d/(?<id>[-\w]+)", RegexOptions.IgnoreCase)]
    private static partial Regex GoogleSheetIdRegex();

    [GeneratedRegex(@"[?&]gid=(?<gid>\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex GoogleSheetGidRegex();

    [GeneratedRegex(@"drive\.google\.com/file/d/(?<id>[-\w]+)", RegexOptions.IgnoreCase)]
    private static partial Regex GoogleDriveFileIdRegex();

    [GeneratedRegex(@"^[-\w]{20,}$", RegexOptions.IgnoreCase)]
    private static partial Regex GoogleBareIdRegex();
}
