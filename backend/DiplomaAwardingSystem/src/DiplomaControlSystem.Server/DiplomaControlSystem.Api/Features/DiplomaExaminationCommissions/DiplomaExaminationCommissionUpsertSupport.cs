using Core.Domain.Entities.StudyGroup;
using Core.Domain.Enums;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using DiplomaControlSystem.Api.Infrastructure.Access;
using DiplomaControlSystem.Api.Infrastructure.Groups;
using Microsoft.EntityFrameworkCore;
using static DiplomaControlSystem.Api.Features.DiplomaExaminationCommissions.DiplomaExaminationCommissionContracts;

namespace DiplomaControlSystem.Api.Features.DiplomaExaminationCommissions;

internal static class DiplomaExaminationCommissionUpsertSupport
{
    internal sealed record ValidatedInput(
        EducationLevel EducationLevel,
        string DefenseYear,
        IReadOnlyCollection<Group> Groups,
        int? HeadTeacherId,
        string? HeadPersonaName,
        string? HeadPersonaPosition);

    public static async Task<Result<ValidatedInput>> ValidateAsync(
        DbDocGenContext context,
        UpsertRequest request,
        SecretaryAccessContext secretary,
        int? commissionId,
        CancellationToken ct)
    {
        _ = Rules.TryParseEducationLevel(request.EducationLevel, out var educationLevel);
        _ = GroupYearRules.TryNormalizeDefenseYear(request.DefenseYear, out var defenseYear);

        var groupIds = request.GroupIds.Distinct().ToList();
        if (groupIds.Count != request.GroupIds.Count)
        {
            return ErrorDetails.Conflict(
                "DiplomaExaminationCommission.DuplicateGroups",
                "Group list must not contain duplicates.");
        }

        var groups = await context.Groups
            .Where(group => groupIds.Contains(group.Id))
            .ToListAsync(ct);

        if (groups.Count != groupIds.Count)
        {
            return ErrorDetails.NotFound(
                "Group.NotFound",
                "One or more groups were not found.");
        }

        if (groups.Any(group => group.SpecialtyId != secretary.SpecialtyId))
        {
            return ErrorDetails.Forbidden(
                "Group.Forbidden",
                "One or more groups do not belong to secretary specialty.");
        }

        if (groups.Any(group => group.EducationLevel != educationLevel))
        {
            return ErrorDetails.Conflict(
                "Group.EducationLevelMismatch",
                "One or more groups do not match selected education level.");
        }

        if (groups.Any(group => !string.Equals(group.Year, defenseYear, StringComparison.Ordinal)))
        {
            return ErrorDetails.Conflict(
                "Group.DefenseYearMismatch",
                "One or more groups do not match selected defense year.");
        }

        if (groups.Any(group => group.DiplomaExaminationCommissionId is not null
                                && group.DiplomaExaminationCommissionId != commissionId))
        {
            return ErrorDetails.Conflict(
                "Group.AlreadyHasCommission",
                "One or more groups are already assigned to another diploma examination commission.");
        }

        var teacherValidationResult = await ValidateTeachersAsync(context, request, secretary.SpecialtyId, ct);
        if (teacherValidationResult.IsFailure)
        {
            return teacherValidationResult.ErrorDetails;
        }

        var duplicateOrderExists = await context.DiplomaExaminationCommissions
            .AsNoTracking()
            .AnyAsync(
                dec => dec.Id != commissionId
                       && dec.OrderNumber == request.OrderNumber
                       && dec.Groups.Any(group =>
                           group.SpecialtyId == secretary.SpecialtyId
                           && group.Year == defenseYear),
                ct);

        if (duplicateOrderExists)
        {
            return ErrorDetails.Conflict(
                "DiplomaExaminationCommission.OrderNumberAlreadyExists",
                "Diploma examination commission with the same order number already exists for this specialty and defense year.");
        }

        return new ValidatedInput(
            educationLevel,
            defenseYear,
            groups,
            request.HeadTeacherId,
            request.HeadPersonaName?.Trim(),
            request.HeadPersonaPosition?.Trim());
    }

    public static async Task<CommissionDto> GetDtoAsync(
        DbDocGenContext context,
        int commissionId,
        string defenseYear,
        CancellationToken ct)
    {
        var commission = await context.DiplomaExaminationCommissions
            .AsNoTracking()
            .Include(dec => dec.Groups)
            .Include(dec => dec.HeadTeacher)
            .Include(dec => dec.FirstMemberTeacher)
            .Include(dec => dec.SecondMemberTeacher)
            .Include(dec => dec.ThirdMemberTeacher)
            .Include(dec => dec.Secretary)
            .FirstAsync(dec => dec.Id == commissionId, ct);

        return Map(commission, defenseYear);
    }

    public static CommissionDto Map(
        Core.Domain.Entities.TeacherStaff.DiplomaExaminationCommission dec,
        string defenseYear)
    {
        return new CommissionDto(
            dec.Id,
            dec.OrderNumber,
            dec.EducationLevel.ToString(),
            GroupYearRules.FormatAcademicYearFromDefenseYear(defenseYear),
            defenseYear,
            dec.StartDate,
            dec.EndDate,
            MapHead(dec),
            new[]
            {
                MapMember(1, dec.FirstMemberTeacher!),
                MapMember(2, dec.SecondMemberTeacher!),
                MapMember(3, dec.ThirdMemberTeacher!)
            },
            new SecretaryDto(dec.Secretary!.Id, dec.Secretary.FullName),
            dec.Groups
                .Where(group => string.Equals(group.Year, defenseYear, StringComparison.Ordinal))
                .OrderBy(group => group.Name, StringComparer.Ordinal)
                .Select(group => new GroupDto(group.Id, group.Name))
                .ToList());
    }

    private static async Task<Result> ValidateTeachersAsync(
        DbDocGenContext context,
        UpsertRequest request,
        int specialtyId,
        CancellationToken ct)
    {
        var teacherIds = new[]
            {
                request.FirstMemberTeacherId,
                request.SecondMemberTeacherId,
                request.ThirdMemberTeacherId
            }
            .Concat(request.HeadTeacherId is null
                ? Array.Empty<int>()
                : new[] { request.HeadTeacherId.Value })
            .ToList();

        if (teacherIds.Distinct().Count() != teacherIds.Count)
        {
            return ErrorDetails.Conflict(
                "DiplomaExaminationCommission.DuplicateMembers",
                "Commission roles must be assigned to different people.");
        }

        var existingTeacherIds = await context.Teachers
            .AsNoTracking()
            .Where(teacher => teacher.SpecialtyId == specialtyId)
            .Where(teacher => teacherIds.Contains(teacher.Id))
            .Select(teacher => teacher.Id)
            .ToListAsync(ct);

        if (existingTeacherIds.Count != teacherIds.Count)
        {
            return ErrorDetails.NotFound(
                "Teacher.NotFound",
                "One or more commission teachers were not found in secretary specialty.");
        }

        return Result.Success();
    }

    private static PersonDto MapHead(Core.Domain.Entities.TeacherStaff.DiplomaExaminationCommission dec)
    {
        if (dec.HeadTeacher is not null)
        {
            return new PersonDto(
                dec.HeadTeacher.Id,
                dec.HeadTeacher.FullName,
                dec.HeadTeacher.ShortName,
                dec.HeadTeacher.Position,
                IsInvited: false);
        }

        return new PersonDto(
            TeacherId: null,
            dec.HeadPersonaName ?? string.Empty,
            ShortName: null,
            dec.HeadPersonaPosition,
            IsInvited: true);
    }

    private static MemberDto MapMember(int order, Core.Domain.Entities.TeacherStaff.Teacher teacher)
    {
        return new MemberDto(order, teacher.Id, teacher.FullName, teacher.ShortName, teacher.Position);
    }
}
