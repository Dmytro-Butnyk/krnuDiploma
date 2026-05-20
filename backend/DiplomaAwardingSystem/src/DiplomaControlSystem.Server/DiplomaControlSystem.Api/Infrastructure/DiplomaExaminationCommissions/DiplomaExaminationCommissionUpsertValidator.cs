using DiplomaControlSystem.Api.Contracts.DiplomaExaminationCommissions;
using DiplomaControlSystem.Api.Infrastructure.Groups;
using FluentValidation;
using static DiplomaControlSystem.Api.Contracts.DiplomaExaminationCommissions.DiplomaExaminationCommissionContracts;

namespace DiplomaControlSystem.Api.Infrastructure.DiplomaExaminationCommissions;

internal abstract class DiplomaExaminationCommissionUpsertValidator<TRequest> : AbstractValidator<TRequest>
    where TRequest : DiplomaExaminationCommissionUpsertRequest
{
    protected DiplomaExaminationCommissionUpsertValidator()
    {
        RuleFor(x => x.SecretaryEmail)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);

        RuleFor(x => x.SecretaryId)
            .GreaterThan(0);

        RuleFor(x => x.OrderNumber)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Order number is required.")
            .MaximumLength(64);

        RuleFor(x => x.EducationLevel)
            .NotEmpty()
            .Must(level => DiplomaExaminationCommissionRules.TryParseEducationLevel(level, out _))
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
            .Must(x => DiplomaExaminationCommissionRules.HasExactlyOneHeadSource(
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
            .Must(x => DiplomaExaminationCommissionRules.DatesBelongToDefenseYear(
                x.StartDate,
                x.EndDate,
                x.DefenseYear))
            .WithMessage("Start and end dates must belong to the selected defense year.");
    }
}
