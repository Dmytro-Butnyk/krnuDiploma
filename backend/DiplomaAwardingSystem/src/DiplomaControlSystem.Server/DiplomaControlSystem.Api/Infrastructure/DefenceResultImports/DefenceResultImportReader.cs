using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.ResultPattern;
using ExcelDataReader;

namespace DiplomaControlSystem.Api.Infrastructure.DefenceResultImports;

internal sealed partial class DefenceResultImportReader(IHttpClientFactory httpClientFactory) : IScopedService
{
    private const int MaxRowsCount = 500;

    private static readonly HashSet<string> StudentFullNameHeaderNames = new(StringComparer.Ordinal)
    {
        "\u041f\u0406\u0411 \u0421\u0422\u0423\u0414\u0415\u041d\u0422\u0410",
        "STUDENT",
        "STUDENT NAME",
        "FULL NAME",
        "FULLNAME"
    };

    private static readonly HashSet<string> PlagiarismHeaderNames = new(StringComparer.Ordinal)
    {
        "\u041f\u0420\u041e\u0426\u0415\u041d\u0422 \u0417\u0410\u041f\u041e\u0417\u0418\u0427\u0415\u041d\u042c",
        "PLAGIARISM PERCENT"
    };

    private static readonly HashSet<string> SupervisorHeaderNames = new(StringComparer.Ordinal)
    {
        "\u041f\u0406\u0411 \u041a\u0415\u0420\u0406\u0412\u041d\u0418\u041a\u0410",
        "\u041a\u0415\u0420\u0406\u0412\u041d\u0418\u041a",
        "SUPERVISOR",
        "SUPERVISOR NAME"
    };

    private static readonly HashSet<string> CommissionScoreHeaderNames = new(StringComparer.Ordinal)
    {
        "\u0417\u0410\u0413\u0410\u041b\u042c\u041d\u0410 \u041e\u0426\u0406\u041d\u041a\u0410",
        "COMMISSION SCORE",
        "TOTAL SCORE"
    };

    private static readonly HashSet<string> SupervisorScoreHeaderNames = new(StringComparer.Ordinal)
    {
        "\u041e\u0426\u0406\u041d\u041a\u0410 \u041a\u0415\u0420\u0406\u0412\u041d\u0418\u041a\u0410",
        "SUPERVISOR SCORE"
    };

    private static readonly HashSet<string> ReviewerScoreHeaderNames = new(StringComparer.Ordinal)
    {
        "\u041e\u0426\u0406\u041d\u041a\u0410 \u0420\u0415\u0426\u0415\u041d\u0417\u0415\u041d\u0422\u0410",
        "REVIEWER SCORE"
    };

    private static readonly HashSet<string> DefenceDateHeaderNames = new(StringComparer.Ordinal)
    {
        "\u0414\u0410\u0422\u0410 \u0417\u0410\u0425\u0418\u0421\u0422\u0423",
        "DEFENCE DATE",
        "DEFENSE DATE"
    };

    static DefenceResultImportReader()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public async Task<Result<IReadOnlyCollection<DefenceResultImportRow>>> ReadAsync(
        IFormFile? resultsFile,
        string? googleDriveUrl,
        CancellationToken ct)
    {
        if (resultsFile is not null)
        {
            await using var stream = resultsFile.OpenReadStream();
            return ReadFromStream(stream, resultsFile.FileName);
        }

        if (!string.IsNullOrWhiteSpace(googleDriveUrl))
        {
            return await ReadFromGoogleUrlAsync(googleDriveUrl, ct);
        }

        return ErrorDetails.Validation(
            "DefenceResultImport.SourceMissing",
            "Results file or Google Sheets URL is required.");
    }

    private async Task<Result<IReadOnlyCollection<DefenceResultImportRow>>> ReadFromGoogleUrlAsync(
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
                "DefenceResultImport.GoogleSourceUnavailable",
                "Google table is unavailable. Check that the link is public.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        return ReadFromStream(stream, exportUrlResult.Value.FileName);
    }

    private static Result<IReadOnlyCollection<DefenceResultImportRow>> ReadFromStream(Stream stream, string fileName)
    {
        using var reader = CreateReader(stream, fileName);
        var rowsResult = ReadRows(reader);
        if (rowsResult.IsFailure)
        {
            return rowsResult.ErrorDetails;
        }

        var rows = rowsResult.Value!;
        if (rows.Count == 0)
        {
            return ErrorDetails.Validation(
                "DefenceResultImport.Empty",
                "Results table does not contain any students.");
        }

        if (rows.Count > MaxRowsCount)
        {
            return ErrorDetails.Validation(
                "DefenceResultImport.TooManyRows",
                string.Create(CultureInfo.InvariantCulture, $"Results row count cannot exceed {MaxRowsCount}."));
        }

        var duplicates = FindDuplicates(rows.Select(row => row.FullName));
        if (duplicates.Count > 0)
        {
            return ErrorDetails.Validation(
                "DefenceResultImport.Duplicates",
                string.Create(CultureInfo.InvariantCulture, $"Results table contains duplicate students: {string.Join(", ", duplicates)}."));
        }

        return rows;
    }

    private static Result<List<DefenceResultImportRow>> ReadRows(IExcelDataReader reader)
    {
        var rows = new List<DefenceResultImportRow>();
        DefenceResultImportColumns? columns = null;

        while (reader.Read())
        {
            if (columns is null)
            {
                columns = TryDetectColumns(reader);
                continue;
            }

            var fullName = NormalizeCellValue(reader.GetValue(columns.StudentFullNameColumnIndex));
            if (string.IsNullOrWhiteSpace(fullName))
            {
                continue;
            }

            var rowResult = CreateRow(reader, columns, fullName);
            if (rowResult.IsFailure)
            {
                return rowResult.ErrorDetails;
            }

            rows.Add(rowResult.Value!);
        }

        if (columns is null)
        {
            return ErrorDetails.Validation(
                "DefenceResultImport.StudentNameHeaderMissing",
                "Results table must contain a student full name column.");
        }

        return rows;
    }

    private static Result<DefenceResultImportRow> CreateRow(
        IExcelDataReader reader,
        DefenceResultImportColumns columns,
        string fullName)
    {
        var plagiarismResult = ReadOptionalPercent(reader, columns.PlagiarismColumnIndex, fullName, "Процент запозичень");
        if (plagiarismResult.IsFailure)
        {
            return plagiarismResult.ErrorDetails;
        }

        var commissionScoreResult = ReadOptionalScore(reader, columns.CommissionScoreColumnIndex, fullName, "Загальна оцінка");
        if (commissionScoreResult.IsFailure)
        {
            return commissionScoreResult.ErrorDetails;
        }

        var supervisorScoreResult = ReadOptionalScore(reader, columns.SupervisorScoreColumnIndex, fullName, "Оцінка керівника");
        if (supervisorScoreResult.IsFailure)
        {
            return supervisorScoreResult.ErrorDetails;
        }

        var reviewerScoreResult = ReadOptionalScore(reader, columns.ReviewerScoreColumnIndex, fullName, "Оцінка рецензента");
        if (reviewerScoreResult.IsFailure)
        {
            return reviewerScoreResult.ErrorDetails;
        }

        return new DefenceResultImportRow(
            fullName,
            GetOptionalCellValue(reader, columns.SupervisorColumnIndex),
            plagiarismResult.Value,
            commissionScoreResult.Value,
            supervisorScoreResult.Value,
            reviewerScoreResult.Value,
            GetOptionalCellValue(reader, columns.DefenceDateColumnIndex));
    }

    private static Result<float?> ReadOptionalPercent(
        IExcelDataReader reader,
        int? columnIndex,
        string fullName,
        string columnName)
    {
        var value = GetOptionalCellValue(reader, columnIndex);
        if (string.IsNullOrWhiteSpace(value))
        {
            return (float?)null;
        }

        var normalized = value.Trim().TrimEnd('%').Replace(',', '.');
        if (!float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            || parsed < 0
            || parsed > 100)
        {
            return ErrorDetails.Validation(
                "DefenceResultImport.PercentInvalid",
                string.Create(CultureInfo.InvariantCulture, $"Column {columnName} for student {fullName} must be a number from 0 to 100."));
        }

        return parsed;
    }

    private static Result<int?> ReadOptionalScore(
        IExcelDataReader reader,
        int? columnIndex,
        string fullName,
        string columnName)
    {
        var value = GetOptionalCellValue(reader, columnIndex);
        if (string.IsNullOrWhiteSpace(value))
        {
            return (int?)null;
        }

        var normalized = value.Trim().Replace(',', '.');
        if (!decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            || parsed < 0
            || parsed > 100
            || parsed != decimal.Truncate(parsed))
        {
            return ErrorDetails.Validation(
                "DefenceResultImport.ScoreInvalid",
                string.Create(CultureInfo.InvariantCulture, $"Column {columnName} for student {fullName} must be an integer from 0 to 100."));
        }

        return (int)parsed;
    }

    private static DefenceResultImportColumns? TryDetectColumns(IExcelDataReader reader)
    {
        int? studentFullNameColumnIndex = null;
        int? supervisorColumnIndex = null;
        int? plagiarismColumnIndex = null;
        int? commissionScoreColumnIndex = null;
        int? supervisorScoreColumnIndex = null;
        int? reviewerScoreColumnIndex = null;
        int? defenceDateColumnIndex = null;

        for (var i = 0; i < reader.FieldCount; i++)
        {
            var value = NormalizeHeaderValue(reader.GetValue(i));
            if (StudentFullNameHeaderNames.Contains(value))
            {
                studentFullNameColumnIndex = i;
            }
            else if (IsSupervisorNameHeader(value))
            {
                supervisorColumnIndex = i;
            }
            else if (PlagiarismHeaderNames.Contains(value))
            {
                plagiarismColumnIndex = i;
            }
            else if (CommissionScoreHeaderNames.Contains(value))
            {
                commissionScoreColumnIndex = i;
            }
            else if (SupervisorScoreHeaderNames.Contains(value))
            {
                supervisorScoreColumnIndex = i;
            }
            else if (ReviewerScoreHeaderNames.Contains(value))
            {
                reviewerScoreColumnIndex = i;
            }
            else if (DefenceDateHeaderNames.Contains(value))
            {
                defenceDateColumnIndex = i;
            }
        }

        return studentFullNameColumnIndex is null
            ? null
            : new DefenceResultImportColumns(
                studentFullNameColumnIndex.Value,
                supervisorColumnIndex,
                plagiarismColumnIndex,
                commissionScoreColumnIndex,
                supervisorScoreColumnIndex,
                reviewerScoreColumnIndex,
                defenceDateColumnIndex);
    }

    private static IExcelDataReader CreateReader(Stream stream, string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return extension.Equals(".csv", StringComparison.OrdinalIgnoreCase)
            ? ExcelReaderFactory.CreateCsvReader(stream)
            : ExcelReaderFactory.CreateReader(stream);
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
        if (SupervisorHeaderNames.Contains(value))
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
                "defence-results.csv");
        }

        var driveMatch = GoogleDriveFileIdRegex().Match(trimmedSource);
        if (driveMatch.Success)
        {
            return new GoogleDownloadSource(
                BuildGoogleDriveDownloadUri(driveMatch.Groups["id"].Value),
                "defence-results.xlsx");
        }

        if (GoogleBareIdRegex().IsMatch(trimmedSource))
        {
            return new GoogleDownloadSource(
                BuildGoogleSheetsCsvExportUri(trimmedSource, gid: null),
                "defence-results.csv");
        }

        return ErrorDetails.Validation(
            "DefenceResultImport.UnsupportedGoogleUrl",
            "Google URL must be a public Google Sheets link or Google Drive file link.");
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

    private sealed record DefenceResultImportColumns(
        int StudentFullNameColumnIndex,
        int? SupervisorColumnIndex,
        int? PlagiarismColumnIndex,
        int? CommissionScoreColumnIndex,
        int? SupervisorScoreColumnIndex,
        int? ReviewerScoreColumnIndex,
        int? DefenceDateColumnIndex);

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
