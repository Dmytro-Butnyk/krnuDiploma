using System.Globalization;
using System.Text.RegularExpressions;

namespace DiplomaControlSystem.Api.Infrastructure.Groups;

internal static partial class GroupYearRules
{
    private const int MaxFutureDefenseYearOffset = 2;

    private static readonly Lazy<TimeZoneInfo> UkraineTimeZone = new(FindUkraineTimeZone);

    public static bool TryNormalizeDefenseYear(string? year, out string normalizedYear)
    {
        normalizedYear = string.Empty;

        if (string.IsNullOrWhiteSpace(year))
        {
            return false;
        }

        var trimmedYear = year.Trim();
        if (!DefenseYearRegex().IsMatch(trimmedYear))
        {
            return false;
        }

        normalizedYear = trimmedYear;
        return true;
    }

    public static bool IsAllowedDefenseYear(string? year)
    {
        return TryNormalizeDefenseYear(year, out var normalizedYear)
               && TryParseDefenseYear(normalizedYear, out var defenseYear)
               && IsAllowedDefenseYear(defenseYear);
    }

    public static string FormatAcademicYearFromDefenseYear(string defenseYear)
    {
        return TryParseDefenseYear(defenseYear, out var parsedDefenseYear)
            ? string.Create(
                CultureInfo.InvariantCulture,
                $"{parsedDefenseYear - 1}/{parsedDefenseYear % 100:00}")
            : defenseYear;
    }

    public static int? GetDefenseYearSortKey(string defenseYear)
    {
        return TryParseDefenseYear(defenseYear, out var parsedDefenseYear)
            ? parsedDefenseYear
            : null;
    }

    public static string GetAllowedDefenseYearRangeMessage()
    {
        var currentYear = GetCurrentUkraineYear();
        return string.Create(
            CultureInfo.InvariantCulture,
            $"Defense year must be between {currentYear} and {currentYear + MaxFutureDefenseYearOffset}.");
    }

    private static bool IsAllowedDefenseYear(int defenseYear)
    {
        var currentYear = GetCurrentUkraineYear();
        return defenseYear >= currentYear && defenseYear <= currentYear + MaxFutureDefenseYearOffset;
    }

    private static int GetCurrentUkraineYear()
    {
        var utcNow = TimeProvider.System.GetUtcNow();
        var ukraineNow = TimeZoneInfo.ConvertTime(utcNow, UkraineTimeZone.Value);
        return ukraineNow.Year;
    }

    private static bool TryParseDefenseYear(string defenseYear, out int parsedDefenseYear)
    {
        return int.TryParse(
            defenseYear,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out parsedDefenseYear);
    }

    private static TimeZoneInfo FindUkraineTimeZone()
    {
        foreach (var timeZoneId in new[] { "Europe/Kyiv", "Europe/Kiev", "FLE Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        throw new TimeZoneNotFoundException("Ukraine time zone was not found.");
    }

    [GeneratedRegex(@"^\d{4}$")]
    private static partial Regex DefenseYearRegex();
}
