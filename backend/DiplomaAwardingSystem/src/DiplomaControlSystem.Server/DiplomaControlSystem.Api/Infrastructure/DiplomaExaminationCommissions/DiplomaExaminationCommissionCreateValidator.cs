using DiplomaControlSystem.Api.Infrastructure.Groups;
using FluentValidation;
using static DiplomaControlSystem.Api.Contracts.DiplomaExaminationCommissions.DiplomaExaminationCommissionContracts;

namespace DiplomaControlSystem.Api.Infrastructure.DiplomaExaminationCommissions;

internal abstract class DiplomaExaminationCommissionCreateValidator<TRequest> : AbstractValidator<TRequest>
    where TRequest : DiplomaExaminationCommissionCreateRequest
{
    protected DiplomaExaminationCommissionCreateValidator()
    {
        Include(new DiplomaExaminationCommissionCommonValidator<TRequest>());

        RuleFor(x => x.EducationLevel)
            .NotEmpty()
            .Must(level => DiplomaExaminationCommissionRules.TryParseEducationLevel(level, out _))
            .WithMessage("Education level must be Bachelor or Master.");

        RuleFor(x => x.DefenseYear)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(year => GroupYearRules.TryNormalizeDefenseYear(year, out _))
            .WithMessage("Defense year must be a 4-digit year like 2026.")
            .Must(GroupYearRules.IsAllowedDefenseYear)
            .WithMessage(_ => GroupYearRules.GetAllowedDefenseYearRangeMessage());

        RuleFor(x => x)
            .Must(x => DiplomaExaminationCommissionRules.DatesBelongToDefenseYear(
                x.StartDate,
                x.EndDate,
                x.DefenseYear))
            .WithMessage("Start and end dates must belong to the selected defense year.");
    }
}
