using System.Globalization;
using Core.Domain.Enums;
using DiplomaControlSystem.Api.Infrastructure.Groups;
using FluentValidation;

namespace DiplomaControlSystem.Api.Features.DiplomaExaminationCommissions;

public static class DiplomaExaminationCommissionContracts
{
    public sealed record GroupDto(int Id, string Name);

    public sealed record TeacherDto(
        int Id,
        string FullName,
        string ShortName,
        string Position);

    public sealed record SecretaryDto(int Id, string FullName);

    public sealed record PersonDto(
        int? TeacherId,
        string FullName,
        string? ShortName,
        string? Position,
        bool IsInvited);

    public sealed record MemberDto(
        int Order,
        int TeacherId,
        string FullName,
        string ShortName,
        string Position);

    public sealed record CommissionDto(
        int Id,
        int OrderNumber,
        string EducationLevel,
        string Year,
        string DefenseYear,
        DateOnly StartDate,
        DateOnly EndDate,
        PersonDto Head,
        IReadOnlyCollection<MemberDto> Members,
        SecretaryDto Secretary,
        IReadOnlyCollection<GroupDto> Groups);

    public sealed class UpsertRequest
    {
        public string SecretaryEmail { get; init; } = string.Empty;
        public int OrderNumber { get; init; }
        public string EducationLevel { get; init; } = string.Empty;
        public string DefenseYear { get; init; } = string.Empty;
        public IReadOnlyCollection<int> GroupIds { get; init; } = Array.Empty<int>();
        public int? HeadTeacherId { get; init; }
        public string? HeadPersonaName { get; init; }
        public string? HeadPersonaPosition { get; init; }
        public int FirstMemberTeacherId { get; init; }
        public int SecondMemberTeacherId { get; init; }
        public int ThirdMemberTeacherId { get; init; }
        public DateOnly StartDate { get; init; }
        public DateOnly EndDate { get; init; }
    }

    internal sealed class UpsertValidator : AbstractValidator<UpsertRequest>
    {
        public UpsertValidator()
        {
            RuleFor(x => x.SecretaryEmail)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(320);

            RuleFor(x => x.OrderNumber)
                .GreaterThan(0);

            RuleFor(x => x.EducationLevel)
                .NotEmpty()
                .Must(level => Rules.TryParseEducationLevel(level, out _))
                .WithMessage("Education level must be Bachelor or Master.");

            RuleFor(x => x.DefenseYear)
                .NotEmpty()
                .Must(year => GroupYearRules.TryNormalizeDefenseYear(year, out _))
                .WithMessage("Defense year must be a 4-digit year like 2026.");

            RuleFor(x => x.GroupIds)
                .NotEmpty()
                .WithMessage("At least one group must be selected.");

            RuleForEach(x => x.GroupIds)
                .GreaterThan(0);

            RuleFor(x => x.HeadTeacherId)
                .GreaterThan(0)
                .When(x => x.HeadTeacherId is not null);

            RuleFor(x => x.HeadPersonaName)
                .MaximumLength(256);

            RuleFor(x => x.HeadPersonaPosition)
                .MaximumLength(256);

            RuleFor(x => x)
                .Must(x => Rules.HasExactlyOneHeadSource(
                    x.HeadTeacherId,
                    x.HeadPersonaName,
                    x.HeadPersonaPosition))
                .WithMessage("Specify either head teacher or invited head name and position.");

            RuleFor(x => x.FirstMemberTeacherId)
                .GreaterThan(0);

            RuleFor(x => x.SecondMemberTeacherId)
                .GreaterThan(0);

            RuleFor(x => x.ThirdMemberTeacherId)
                .GreaterThan(0);

            RuleFor(x => x.EndDate)
                .GreaterThanOrEqualTo(x => x.StartDate);

            RuleFor(x => x)
                .Must(x => Rules.DatesBelongToDefenseYear(
                    x.StartDate,
                    x.EndDate,
                    x.DefenseYear))
                .WithMessage("Start and end dates must belong to the selected defense year.");
        }
    }

    internal static class Rules
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
            if (!GroupYearRules.TryNormalizeDefenseYear(defenseYear, out normalizedDefenseYear))
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

        public static bool HasExactlyOneHeadSource(int? headTeacherId, string? headPersonaName, string? headPersonaPosition)
        {
            var hasTeacher = headTeacherId is not null;
            var hasPersonaName = !string.IsNullOrWhiteSpace(headPersonaName);
            var hasPersonaPosition = !string.IsNullOrWhiteSpace(headPersonaPosition);

            return hasTeacher
                ? !hasPersonaName && !hasPersonaPosition
                : hasPersonaName && hasPersonaPosition;
        }
    }
}
