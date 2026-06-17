using Core.Domain.Entities.StudyGroup;
using Core.Domain.Entities.TeacherStaff;
using Core.Domain.Enums;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using DiplomaControlSystem.Api.Infrastructure.Access;
using DiplomaControlSystem.Api.Infrastructure.AcademicYears;
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
        _ = AcademicYearRules.TryNormalizeDefenseYear(request.DefenseYear, out var defenseYear);
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
        CancellationToken ct)
    {
        var commissionHead = await context.CommissionHeads
            .AsNoTracking()
            .Where(head => head.Id == commissionHeadId)
            .Select(head => new
            {
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
        var consultantIds = new[]
            {
                request.FirstConsultantId,
                request.SecondConsultantId
            }
            .Where(id => id is not null)
            .Select(id => id!.Value)
            .ToArray();

        if (teacherIds.Distinct().Count() != teacherIds.Length)
        {
            return ErrorDetails.Conflict(
                "DiplomaExaminationCommission.DuplicateMembers",
                "Commission roles must be assigned to different teachers.");
        }

        if (consultantIds.Distinct().Count() != consultantIds.Length)
        {
            return ErrorDetails.Conflict(
                "DiplomaExaminationCommission.DuplicateConsultants",
                "Consultant roles must be assigned to different teachers.");
        }

        var allTeacherIds = teacherIds
            .Concat(consultantIds)
            .Distinct()
            .ToArray();

        var existingTeacherIds = await context.Teachers
            .AsNoTracking()
            .Where(teacher => teacher.SpecialtyId == specialtyId)
            .Where(teacher => teacher.IsActive)
            .Where(teacher => allTeacherIds.Contains(teacher.Id))
            .Select(teacher => teacher.Id)
            .ToListAsync(ct);

        if (existingTeacherIds.Count != allTeacherIds.Length)
        {
            return ErrorDetails.NotFound(
                "Teacher.NotFound",
                "One or more commission teachers or consultants were not found in secretary specialty.");
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
}
