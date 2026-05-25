using FluentValidation;
using static DiplomaControlSystem.Api.Contracts.DiplomaExaminationCommissions.DiplomaExaminationCommissionContracts;

namespace DiplomaControlSystem.Api.Infrastructure.DiplomaExaminationCommissions;

internal sealed class DiplomaExaminationCommissionCommonValidator<TRequest> : AbstractValidator<TRequest>
    where TRequest : DiplomaExaminationCommissionUpdateRequest
{
    public DiplomaExaminationCommissionCommonValidator()
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

        RuleFor(x => x.CommissionHeadId)
            .GreaterThan(0);

        RuleFor(x => x.FirstMemberTeacherId)
            .GreaterThan(0);

        RuleFor(x => x.SecondMemberTeacherId)
            .GreaterThan(0);

        RuleFor(x => x.ThirdMemberTeacherId)
            .GreaterThan(0);

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate);
    }
}
