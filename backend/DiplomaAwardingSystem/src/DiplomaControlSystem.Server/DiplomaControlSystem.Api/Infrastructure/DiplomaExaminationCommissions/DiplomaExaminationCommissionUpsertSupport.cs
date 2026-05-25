using Core.Domain.Entities.StudyGroup;
using Core.Domain.Entities.TeacherStaff;
using Core.Domain.Enums;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using DiplomaControlSystem.Api.Infrastructure.Access;
using DiplomaControlSystem.Api.Infrastructure.Groups;
using Microsoft.EntityFrameworkCore;
using static DiplomaControlSystem.Api.Contracts.DiplomaExaminationCommissions.DiplomaExaminationCommissionContracts;

namespace DiplomaControlSystem.Api.Infrastructure.DiplomaExaminationCommissions;

internal static class DiplomaExaminationCommissionUpsertSupport
{
    internal sealed record ValidatedCreateInput(
        EducationLevel EducationLevel,
        string DefenseYear,
        string OrderNumber,
        IReadOnlyCollection<Group> Groups,
        int SpecialtyId,
        int SecretaryId,
        int CommissionHeadId);

    internal sealed record ValidatedUpdateInput(
        string OrderNumber,
        int SecretaryId,
        int CommissionHeadId);

    public static async Task<Result<ValidatedCreateInput>> ValidateCreateAsync(
        DbDocGenContext context,
        DiplomaExaminationCommissionCreateRequest request,
        SecretaryAccessContext secretary,
        CancellationToken ct)
    {
        _ = DiplomaExaminationCommissionRules.TryParseEducationLevel(request.EducationLevel, out var educationLevel);
        _ = GroupYearRules.TryNormalizeDefenseYear(request.DefenseYear, out var defenseYear);
        var orderNumber = request.OrderNumber.Trim();

        var commonResult = await ValidateCommonAsync(context, request, secretary, ct);
        if (commonResult.IsFailure)
        {
            return commonResult.ErrorDetails;
        }

        var duplicateCommissionExists = await context.DiplomaExaminationCommissions
            .AsNoTracking()
            .AnyAsync(
                dec => dec.DefenseYear == defenseYear
                       && dec.SpecialtyId == secretary.SpecialtyId
                       && dec.EducationLevel == educationLevel,
                ct);

        if (duplicateCommissionExists)
        {
            return ErrorDetails.Conflict(
                "DiplomaExaminationCommission.AlreadyExists",
                "Diploma examination commission already exists for this defense year, specialty, and education level.");
        }

        var duplicateOrderExists = await context.DiplomaExaminationCommissions
            .AsNoTracking()
            .AnyAsync(
                dec => dec.DefenseYear == defenseYear
                       && dec.SpecialtyId == secretary.SpecialtyId
                       && dec.OrderNumber == orderNumber,
                ct);

        if (duplicateOrderExists)
        {
            return ErrorDetails.Conflict(
                "DiplomaExaminationCommission.OrderNumberAlreadyExists",
                "Diploma examination commission with the same order number already exists for this specialty and defense year.");
        }

        var groups = await context.Groups
            .Where(group => group.SpecialtyId == secretary.SpecialtyId)
            .Where(group => group.EducationLevel == educationLevel)
            .Where(group => group.Year == defenseYear)
            .ToListAsync(ct);

        if (groups.Count == 0)
        {
            return ErrorDetails.NotFound(
                "Group.NotFound",
                "No groups were found for this specialty, education level, and defense year.");
        }

        if (groups.Any(group => group.DiplomaExaminationCommissionId is not null))
        {
            return ErrorDetails.Conflict(
                "Group.AlreadyHasCommission",
                "One or more groups are already assigned to another diploma examination commission.");
        }

        return new ValidatedCreateInput(
            educationLevel,
            defenseYear,
            orderNumber,
            groups,
            secretary.SpecialtyId,
            request.SecretaryId,
            request.CommissionHeadId);
    }

    public static async Task<Result<ValidatedUpdateInput>> ValidateUpdateAsync(
        DbDocGenContext context,
        DiplomaExaminationCommissionUpdateRequest request,
        DiplomaExaminationCommission commission,
        SecretaryAccessContext secretary,
        CancellationToken ct)
    {
        if (!DiplomaExaminationCommissionRules.DatesBelongToDefenseYear(
                request.StartDate,
                request.EndDate,
                commission.DefenseYear))
        {
            return ErrorDetails.Validation(
                "DiplomaExaminationCommission.InvalidDates",
                "Start and end dates must belong to the commission defense year.");
        }

        var commonResult = await ValidateCommonAsync(context, request, secretary, ct);
        if (commonResult.IsFailure)
        {
            return commonResult.ErrorDetails;
        }

        var orderNumber = request.OrderNumber.Trim();
        var duplicateOrderExists = await context.DiplomaExaminationCommissions
            .AsNoTracking()
            .AnyAsync(
                dec => dec.Id != commission.Id
                       && dec.DefenseYear == commission.DefenseYear
                       && dec.SpecialtyId == commission.SpecialtyId
                       && dec.OrderNumber == orderNumber,
                ct);

        if (duplicateOrderExists)
        {
            return ErrorDetails.Conflict(
                "DiplomaExaminationCommission.OrderNumberAlreadyExists",
                "Diploma examination commission with the same order number already exists for this specialty and defense year.");
        }

        return new ValidatedUpdateInput(
            orderNumber,
            request.SecretaryId,
            request.CommissionHeadId);
    }

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
            .Include(dec => dec.SecondMemberTeacher)
            .Include(dec => dec.ThirdMemberTeacher)
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
            GroupYearRules.FormatAcademicYearFromDefenseYear(dec.DefenseYear),
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

    private static async Task<Result> ValidateCommonAsync(
        DbDocGenContext context,
        DiplomaExaminationCommissionUpdateRequest request,
        SecretaryAccessContext secretary,
        CancellationToken ct)
    {
        var assignedSecretaryResult = await ValidateAssignedSecretaryAsync(
            context,
            request.SecretaryId,
            secretary.SpecialtyId,
            ct);

        if (assignedSecretaryResult.IsFailure)
        {
            return assignedSecretaryResult.ErrorDetails;
        }

        var commissionHeadResult = await ValidateCommissionHeadAsync(
            context,
            request.CommissionHeadId,
            secretary.SpecialtyName,
            ct);

        if (commissionHeadResult.IsFailure)
        {
            return commissionHeadResult.ErrorDetails;
        }

        return await ValidateTeachersAsync(context, request, secretary.SpecialtyId, ct);
    }

    private static async Task<Result> ValidateCommissionHeadAsync(
        DbDocGenContext context,
        int commissionHeadId,
        string specialtyName,
        CancellationToken ct)
    {
        var commissionHead = await context.CommissionHeads
            .AsNoTracking()
            .Where(head => head.Id == commissionHeadId)
            .Select(head => new
            {
                head.Specialty,
                head.IsDeleted
            })
            .FirstOrDefaultAsync(ct);

        if (commissionHead is null)
        {
            return ErrorDetails.NotFound(
                "CommissionHead.NotFound",
                "Commission head was not found.");
        }

        if (commissionHead.IsDeleted)
        {
            return ErrorDetails.Conflict(
                "CommissionHead.Deleted",
                "Deleted commission head cannot be used.");
        }

        if (!string.Equals(commissionHead.Specialty, specialtyName, StringComparison.OrdinalIgnoreCase))
        {
            return ErrorDetails.Forbidden(
                "CommissionHead.Forbidden",
                "Commission head does not belong to secretary specialty.");
        }

        return Result.Success();
    }

    private static async Task<Result> ValidateTeachersAsync(
        DbDocGenContext context,
        DiplomaExaminationCommissionUpdateRequest request,
        int specialtyId,
        CancellationToken ct)
    {
        var teacherIds = new[]
        {
            request.FirstMemberTeacherId,
            request.SecondMemberTeacherId,
            request.ThirdMemberTeacherId
        };

        if (teacherIds.Distinct().Count() != teacherIds.Length)
        {
            return ErrorDetails.Conflict(
                "DiplomaExaminationCommission.DuplicateMembers",
                "Commission roles must be assigned to different teachers.");
        }

        var existingTeacherIds = await context.Teachers
            .AsNoTracking()
            .Where(teacher => teacher.SpecialtyId == specialtyId)
            .Where(teacher => teacherIds.Contains(teacher.Id))
            .Select(teacher => teacher.Id)
            .ToListAsync(ct);

        if (existingTeacherIds.Count != teacherIds.Length)
        {
            return ErrorDetails.NotFound(
                "Teacher.NotFound",
                "One or more commission teachers were not found in secretary specialty.");
        }

        return Result.Success();
    }

    private static async Task<Result> ValidateAssignedSecretaryAsync(
        DbDocGenContext context,
        int secretaryId,
        int specialtyId,
        CancellationToken ct)
    {
        var assignedSecretary = await context.Secretaries
            .AsNoTracking()
            .Where(secretary => secretary.Id == secretaryId)
            .Select(secretary => new
            {
                secretary.SpecialtyId,
                secretary.IsActive
            })
            .FirstOrDefaultAsync(ct);

        if (assignedSecretary is null)
        {
            return ErrorDetails.NotFound(
                "Secretary.NotFound",
                "Selected secretary was not found.");
        }

        if (!assignedSecretary.IsActive)
        {
            return ErrorDetails.Forbidden(
                "Secretary.Inactive",
                "Selected secretary is inactive.");
        }

        if (assignedSecretary.SpecialtyId != specialtyId)
        {
            return ErrorDetails.Forbidden(
                "Secretary.Forbidden",
                "Selected secretary does not belong to secretary specialty.");
        }

        return Result.Success();
    }

    private static CommissionHeadDto MapHead(CommissionHead head)
    {
        return new CommissionHeadDto(
            head.Id,
            head.FullName,
            head.Position,
            head.Company,
            head.Specialty,
            head.IsDeleted);
    }

    private static MemberDto MapMember(Teacher teacher)
    {
        return new MemberDto(teacher.Id, teacher.FullName, teacher.Position);
    }
}
