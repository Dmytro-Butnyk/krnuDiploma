using Core.Domain.ResultPattern;
using Core.Infrastructure;
using Microsoft.EntityFrameworkCore;
using static DiplomaControlSystem.Api.Contracts.DiplomaExaminationCommissions.DiplomaExaminationCommissionContracts;

namespace DiplomaControlSystem.Api.Features.CommissionHeads;

internal static class CommissionHeadRequestSupport
{
    public sealed record NormalizedCommissionHead(
        string FullName,
        string Position,
        string Company,
        string Specialty);

    public static NormalizedCommissionHead Normalize(ICommissionHeadRequest request)
    {
        return new NormalizedCommissionHead(
            request.FullName.Trim(),
            request.Position.Trim(),
            request.Company.Trim(),
            request.Specialty.Trim());
    }

    public static Result ValidateSpecialty(string specialty, string secretarySpecialty)
    {
        return string.Equals(specialty, secretarySpecialty, StringComparison.OrdinalIgnoreCase)
            ? Result.Success()
            : ErrorDetails.Forbidden(
                "CommissionHead.Forbidden",
                "Commission head specialty must match secretary specialty.");
    }

    public static Task<bool> ActiveDuplicateExistsAsync(
        DbDocGenContext context,
        NormalizedCommissionHead normalized,
        int? exceptId,
        CancellationToken ct)
    {
        return context.CommissionHeads
            .AsNoTracking()
            .Where(head => !head.IsDeleted)
            .Where(head => exceptId == null || head.Id != exceptId)
            .AnyAsync(
                head => EF.Functions.ILike(head.FullName, normalized.FullName)
                        && EF.Functions.ILike(head.Position, normalized.Position)
                        && EF.Functions.ILike(head.Company, normalized.Company)
                        && EF.Functions.ILike(head.Specialty, normalized.Specialty),
                ct);
    }

    public static CommissionHeadDto Map(Core.Domain.Entities.TeacherStaff.CommissionHead head)
    {
        return new CommissionHeadDto(
            head.Id,
            head.FullName,
            head.Position,
            head.Company,
            head.Specialty,
            head.IsDeleted);
    }
}
