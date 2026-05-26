using System.Globalization;
using Core.Domain.Enums;
using DiplomaControlSystem.Api.Infrastructure.AcademicYears;

namespace DiplomaControlSystem.Api.Infrastructure.DiplomaExaminationCommissions;

internal static class DiplomaExaminationCommissionRules
{
    public static bool TryParseEducationLevel(string? educationLevel, out EducationLevel parsedEducationLevel)
    {
        parsedEducationLevel = EducationLevel.None;
        return !string.IsNullOrWhiteSpace(educationLevel)
               && Enum.TryParse(educationLevel, ignoreCase: true, out parsedEducationLevel)
               && parsedEducationLevel != EducationLevel.None;
    }

    public static bool TryParseDefenseYear(string? defenseYear, out string normalizedDefenseYear, out int parsedDefenseYear)
    {
        parsedDefenseYear = 0;
        if (!AcademicYearRules.TryNormalizeDefenseYear(defenseYear, out normalizedDefenseYear))
        {
            return false;
        }

        return int.TryParse(
            normalizedDefenseYear,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out parsedDefenseYear);
    }

    public static bool DatesBelongToDefenseYear(DateOnly startDate, DateOnly endDate, string defenseYear)
    {
        return TryParseDefenseYear(defenseYear, out _, out var parsedDefenseYear)
               && startDate.Year == parsedDefenseYear
               && endDate.Year == parsedDefenseYear;
    }
}
