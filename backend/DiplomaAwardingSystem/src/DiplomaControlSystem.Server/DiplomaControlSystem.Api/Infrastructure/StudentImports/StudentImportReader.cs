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

    private static readonly HashSet<string> HeaderNames = new(StringComparer.Ordinal)
    {
        "FULLNAME",
        "FULL NAME",
        "STUDENT",
        "STUDENT NAME",
        "NAME",
        "\u041f\u0406\u0411",
        "\u0424\u0418\u041e",
        "\u0406\u041c'\u042f \u0421\u0422\u0423\u0414\u0415\u041d\u0422\u0410",
        "\u0406\u041c\u042f \u0421\u0422\u0423\u0414\u0415\u041d\u0422\u0410",
        "\u0418\u041c\u042f \u0421\u0422\u0423\u0414\u0415\u041d\u0422\u0410",
        "\u041f\u0420\u0406\u0417\u0412\u0418\u0429\u0415 \u0406\u041c'\u042f \u041f\u041e \u0411\u0410\u0422\u042c\u041a\u041e\u0412\u0406",
        "\u041f\u0420\u0406\u0417\u0412\u0418\u0429\u0415 \u0406\u041c\u042f \u041f\u041e \u0411\u0410\u0422\u042c\u041A\u041E\u0412\u0406"
    };

    static StudentImportReader()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public async Task<Result<IReadOnlyCollection<string>>> ReadAsync(
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

    private async Task<Result<IReadOnlyCollection<string>>> ReadFromGoogleUrlAsync(
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

    private static Result<IReadOnlyCollection<string>> ReadFromStream(Stream stream, string fileName)
    {
        using var reader = CreateReader(stream, fileName);
        var names = ReadStudentNames(reader);

        if (names.Count == 0)
        {
            return ErrorDetails.Validation(
                "StudentImport.Empty",
                "Students table does not contain any names.");
        }

        if (names.Count > MaxStudentsCount)
        {
            return ErrorDetails.Validation(
                "StudentImport.TooManyStudents",
                string.Create(CultureInfo.InvariantCulture, $"Students count cannot exceed {MaxStudentsCount}."));
        }

        var duplicates = FindDuplicates(names);
        if (duplicates.Count > 0)
        {
            return ErrorDetails.Validation(
                "StudentImport.Duplicates",
                string.Create(CultureInfo.InvariantCulture, $"Students table contains duplicate names: {string.Join(", ", duplicates)}."));
        }

        return names;
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

    private static List<string> ReadStudentNames(IExcelDataReader reader)
    {
        var students = new List<string>();
        var selectedColumnIndex = 0;
        var hasDetectedHeader = false;

        while (reader.Read())
        {
            if (!hasDetectedHeader)
            {
                var headerColumnIndex = TryDetectHeader(reader);
                if (headerColumnIndex is not null)
                {
                    selectedColumnIndex = headerColumnIndex.Value;
                    hasDetectedHeader = true;
                    continue;
                }

                hasDetectedHeader = true;
            }

            var fullName = NormalizeCellValue(reader.GetValue(selectedColumnIndex));
            if (!string.IsNullOrWhiteSpace(fullName))
            {
                students.Add(fullName);
            }
        }

        return students;
    }

    private static int? TryDetectHeader(IExcelDataReader reader)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            var value = NormalizeHeaderValue(reader.GetValue(i));
            if (HeaderNames.Contains(value))
            {
                return i;
            }
        }

        return null;
    }

    private static string NormalizeCellValue(object? value)
    {
        return Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
    }

    private static string NormalizeHeaderValue(object? value)
    {
        return NormalizeCellValue(value).ToUpperInvariant();
    }

    private static List<string> FindDuplicates(List<string> names)
    {
        var uniqueNames = new HashSet<string>(names.Count, StringComparer.OrdinalIgnoreCase);
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

    private sealed record GoogleDownloadSource(Uri Url, string FileName);

    [GeneratedRegex(@"docs\.google\.com/spreadsheets/d/(?<id>[-\w]+)", RegexOptions.IgnoreCase)]
    private static partial Regex GoogleSheetIdRegex();

    [GeneratedRegex(@"[?&]gid=(?<gid>\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex GoogleSheetGidRegex();

    [GeneratedRegex(@"drive\.google\.com/file/d/(?<id>[-\w]+)", RegexOptions.IgnoreCase)]
    private static partial Regex GoogleDriveFileIdRegex();

    [GeneratedRegex(@"^[-\w]{20,}$", RegexOptions.IgnoreCase)]
    private static partial Regex GoogleBareIdRegex();
}
