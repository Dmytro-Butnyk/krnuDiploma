using Core.Domain.DependencyInjectionInterfaces;
using Core.Infrastructure;
using DiplomaControlSystem.Api.Contracts.Common;
using Microsoft.EntityFrameworkCore;

namespace DiplomaControlSystem.Api.Infrastructure.Students;

public sealed class DefenceQuestionAuthorOptionsProvider(DbDocGenContext context) : IScopedService
{
    public async Task<IReadOnlyCollection<DefenceQuestionAuthorOptionDto>> GetByStudentIdAsync(
        int studentId,
        CancellationToken ct)
    {
        var commission = await context.Students
            .AsNoTracking()
            .Where(s => s.Id == studentId)
            .Select(s => new CommissionAuthorProjection
            {
                CommissionHeadFullName = s.Group != null
                    && s.Group.DiplomaExaminationCommission != null
                    && s.Group.DiplomaExaminationCommission.CommissionHead != null
                        ? s.Group.DiplomaExaminationCommission.CommissionHead.FullName
                        : null,
                FirstMemberFullName = s.Group != null
                    && s.Group.DiplomaExaminationCommission != null
                    && s.Group.DiplomaExaminationCommission.FirstMemberTeacher != null
                        ? s.Group.DiplomaExaminationCommission.FirstMemberTeacher.FullName
                        : null,
                FirstMemberShortName = s.Group != null
                    && s.Group.DiplomaExaminationCommission != null
                    && s.Group.DiplomaExaminationCommission.FirstMemberTeacher != null
                        ? s.Group.DiplomaExaminationCommission.FirstMemberTeacher.ShortName
                        : null,
                SecondMemberFullName = s.Group != null
                    && s.Group.DiplomaExaminationCommission != null
                    && s.Group.DiplomaExaminationCommission.SecondMemberTeacher != null
                        ? s.Group.DiplomaExaminationCommission.SecondMemberTeacher.FullName
                        : null,
                SecondMemberShortName = s.Group != null
                    && s.Group.DiplomaExaminationCommission != null
                    && s.Group.DiplomaExaminationCommission.SecondMemberTeacher != null
                        ? s.Group.DiplomaExaminationCommission.SecondMemberTeacher.ShortName
                        : null,
                ThirdMemberFullName = s.Group != null
                    && s.Group.DiplomaExaminationCommission != null
                    && s.Group.DiplomaExaminationCommission.ThirdMemberTeacher != null
                        ? s.Group.DiplomaExaminationCommission.ThirdMemberTeacher.FullName
                        : null,
                ThirdMemberShortName = s.Group != null
                    && s.Group.DiplomaExaminationCommission != null
                    && s.Group.DiplomaExaminationCommission.ThirdMemberTeacher != null
                        ? s.Group.DiplomaExaminationCommission.ThirdMemberTeacher.ShortName
                        : null,
                SecretaryFullName = s.Group != null
                    && s.Group.DiplomaExaminationCommission != null
                    && s.Group.DiplomaExaminationCommission.Secretary != null
                        ? s.Group.DiplomaExaminationCommission.Secretary.FullName
                        : null
            })
            .FirstOrDefaultAsync(ct);

        if (commission is null)
        {
            return [];
        }

        var options = new List<DefenceQuestionAuthorOptionDto>();
        var shortNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        AddOption(
            options,
            shortNames,
            BuildSurnameInitials(commission.CommissionHeadFullName),
            commission.CommissionHeadFullName,
            "Голова комісії");
        AddOption(
            options,
            shortNames,
            commission.FirstMemberShortName,
            commission.FirstMemberFullName,
            "Перший член комісії");
        AddOption(
            options,
            shortNames,
            commission.SecondMemberShortName,
            commission.SecondMemberFullName,
            "Другий член комісії");
        AddOption(
            options,
            shortNames,
            commission.ThirdMemberShortName,
            commission.ThirdMemberFullName,
            "Третій член комісії");
        AddOption(
            options,
            shortNames,
            BuildSurnameInitials(commission.SecretaryFullName),
            commission.SecretaryFullName,
            "Секретар комісії");

        return options;
    }

    public static string? GetCanonicalShortName(
        IReadOnlyCollection<DefenceQuestionAuthorOptionDto> options,
        string askedBy)
    {
        var normalizedAskedBy = askedBy.Trim();
        var author = options.FirstOrDefault(option =>
            string.Equals(option.ShortName, normalizedAskedBy, StringComparison.OrdinalIgnoreCase)
            || string.Equals(option.FullName, normalizedAskedBy, StringComparison.OrdinalIgnoreCase));

        return author?.ShortName;
    }

    private static void AddOption(
        List<DefenceQuestionAuthorOptionDto> options,
        HashSet<string> shortNames,
        string? shortName,
        string? fullName,
        string role)
    {
        var normalizedFullName = fullName?.Trim();
        var normalizedShortName = string.IsNullOrWhiteSpace(shortName)
            ? normalizedFullName
            : shortName.Trim();

        if (string.IsNullOrWhiteSpace(normalizedShortName) || !shortNames.Add(normalizedShortName))
        {
            return;
        }

        options.Add(new DefenceQuestionAuthorOptionDto(
            normalizedShortName,
            normalizedFullName ?? normalizedShortName,
            role));
    }

    private static string? BuildSurnameInitials(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return null;
        }

        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 2)
        {
            return fullName.Trim();
        }

        var initials = string.Concat(parts.Skip(1).Select(part => char.ToUpperInvariant(part[0]) + "."));
        return string.IsNullOrWhiteSpace(initials)
            ? parts[0]
            : parts[0] + " " + initials;
    }

    private sealed class CommissionAuthorProjection
    {
        public string? CommissionHeadFullName { get; init; }
        public string? FirstMemberFullName { get; init; }
        public string? FirstMemberShortName { get; init; }
        public string? SecondMemberFullName { get; init; }
        public string? SecondMemberShortName { get; init; }
        public string? ThirdMemberFullName { get; init; }
        public string? ThirdMemberShortName { get; init; }
        public string? SecretaryFullName { get; init; }
    }
}
