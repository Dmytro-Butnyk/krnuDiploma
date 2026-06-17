using FluentValidation;
using static DiplomaControlSystem.Api.Contracts.DiplomaExaminationCommissions.DiplomaExaminationCommissionContracts;

namespace DiplomaControlSystem.Api.Infrastructure.DiplomaExaminationCommissions;

internal sealed class DiplomaExaminationCommissionCommonValidator<TRequest> : AbstractValidator<TRequest>
    where TRequest : DiplomaExaminationCommissionUpdateRequest
{
    private const string TimeFormatPattern = "^(?:[01]\\d|2[0-3]):[0-5]\\d$";

    public DiplomaExaminationCommissionCommonValidator()
    {
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

        RuleFor(x => x.FirstConsultantId)
            .GreaterThan(0)
            .When(x => x.FirstConsultantId is not null);

        RuleFor(x => x.SecondConsultantId)
            .GreaterThan(0)
            .When(x => x.SecondConsultantId is not null);

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate);

        RuleFor(x => x.MeetingStart)
            .NotEmpty()
            .Matches(TimeFormatPattern)
            .WithMessage("Meeting start must be in 24-hour HH:mm format.");

        RuleFor(x => x.MeetingEnd)
            .NotEmpty()
            .Matches(TimeFormatPattern)
            .WithMessage("Meeting end must be in 24-hour HH:mm format.");

        RuleFor(x => x)
            .Must(x => string.CompareOrdinal(x.MeetingEnd, x.MeetingStart) > 0)
            .When(x => IsValidTime(x.MeetingStart) && IsValidTime(x.MeetingEnd))
            .WithMessage("Meeting end must be later than meeting start.");
    }

    private static bool IsValidTime(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
               && System.Text.RegularExpressions.Regex.IsMatch(value, TimeFormatPattern);
    }
}
