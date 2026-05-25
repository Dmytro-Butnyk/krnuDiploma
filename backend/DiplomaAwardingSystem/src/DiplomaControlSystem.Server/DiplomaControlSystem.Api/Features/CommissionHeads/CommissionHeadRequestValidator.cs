using FluentValidation;

namespace DiplomaControlSystem.Api.Features.CommissionHeads;

internal sealed class CommissionHeadRequestValidator<TRequest> : AbstractValidator<TRequest>
    where TRequest : ICommissionHeadRequest
{
    public CommissionHeadRequestValidator()
    {
        RuleFor(x => x.SecretaryEmail)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);

        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.Position)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.Company)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.Specialty)
            .NotEmpty()
            .MaximumLength(256);
    }
}
