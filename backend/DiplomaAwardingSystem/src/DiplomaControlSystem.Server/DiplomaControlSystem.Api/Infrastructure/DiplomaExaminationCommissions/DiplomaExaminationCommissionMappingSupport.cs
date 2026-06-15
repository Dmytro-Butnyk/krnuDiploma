using Core.Domain.Entities.TeacherStaff;
using Core.Infrastructure;
using DiplomaControlSystem.Api.Contracts.Common;
using DiplomaControlSystem.Api.Infrastructure.AcademicYears;
using Microsoft.EntityFrameworkCore;
using static DiplomaControlSystem.Api.Contracts.CommissionHeads.CommissionHeadContracts;
using static DiplomaControlSystem.Api.Contracts.DiplomaExaminationCommissions.DiplomaExaminationCommissionContracts;

namespace DiplomaControlSystem.Api.Infrastructure.DiplomaExaminationCommissions;

internal static class DiplomaExaminationCommissionMappingSupport
{
    public static async Task<DiplomaExaminationCommissionResponse> GetDtoAsync(
        DbDocGenContext context,
        int commissionId,
        CancellationToken ct)
    {
        var commission = await context.DiplomaExaminationCommissions
            .AsNoTracking()
            .Include(dec => dec.Groups)
            .Include(dec => dec.CommissionHead)
            .Include(dec => dec.FirstMemberTeacher)
            .ThenInclude(teacher => teacher!.TeacherPosition)
            .Include(dec => dec.SecondMemberTeacher)
            .ThenInclude(teacher => teacher!.TeacherPosition)
            .Include(dec => dec.ThirdMemberTeacher)
            .ThenInclude(teacher => teacher!.TeacherPosition)
            .Include(dec => dec.Secretary)
            .FirstAsync(dec => dec.Id == commissionId, ct);

        return Map(commission);
    }

    public static DiplomaExaminationCommissionResponse Map(DiplomaExaminationCommission dec)
    {
        return new DiplomaExaminationCommissionResponse(
            dec.Id,
            dec.OrderNumber,
            dec.EducationLevel.ToString(),
            AcademicYearRules.FormatAcademicYearFromDefenseYear(dec.DefenseYear),
            dec.DefenseYear,
            dec.StartDate,
            dec.EndDate,
            MapHead(dec.CommissionHead!),
            new[]
            {
                MapMember(dec.FirstMemberTeacher!),
                MapMember(dec.SecondMemberTeacher!),
                MapMember(dec.ThirdMemberTeacher!)
            },
            new SecretaryDto(dec.Secretary!.Id, dec.Secretary.FullName),
            dec.Groups
                .Where(group => string.Equals(group.Year, dec.DefenseYear, StringComparison.Ordinal))
                .OrderBy(group => group.Name, StringComparer.Ordinal)
                .Select(group => new GroupDto(group.Id, group.Name))
                .ToList());
    }

    private static CommissionHeadDto MapHead(CommissionHead head)
    {
        return new CommissionHeadDto(
            head.Id,
            head.FullName,
            PersonNameFormsDto.From(head.NameForms),
            head.Position,
            head.Company,
            head.Specialty,
            head.IsDeleted);
    }

    private static MemberDto MapMember(Teacher teacher)
    {
        return new MemberDto(
            teacher.Id,
            teacher.FullName,
            teacher.TeacherPosition?.FullName ?? string.Empty);
    }
}
