using Core.Domain.Enums;

namespace DiplomaControlSystem.Api.Infrastructure.Students;

internal static class DefenceGradeCalculator
{
    public static EctsGrade CalculateEctsGrade(int commissionScore)
    {
        return commissionScore switch
        {
            >= 90 and <= 100 => EctsGrade.A,
            >= 82 and <= 89 => EctsGrade.B,
            >= 74 and <= 81 => EctsGrade.C,
            >= 64 and <= 73 => EctsGrade.D,
            >= 60 and <= 63 => EctsGrade.E,
            _ => EctsGrade.None
        };
    }

    public static NationalGrade CalculateNationalGrade(int commissionScore)
    {
        return commissionScore switch
        {
            >= 90 and <= 100 => NationalGrade.Excellent,
            >= 74 and <= 89 => NationalGrade.Good,
            >= 60 and <= 73 => NationalGrade.Satisfactory,
            _ => NationalGrade.None
        };
    }
}
