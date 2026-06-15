using FluentValidation;

namespace DiplomaControlSystem.Api.Infrastructure.CommissionHeads;

internal sealed class CommissionHeadRequestValidator<TRequest> : AbstractValidator<TRequest>
    where TRequest : ICommissionHeadRequest
{
    public CommissionHeadRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.NameForms!.Nominative).MaximumLength(256).When(x => x.NameForms is not null);
        RuleFor(x => x.NameForms!.Genitive).MaximumLength(256).When(x => x.NameForms is not null);
        RuleFor(x => x.NameForms!.Dative).MaximumLength(256).When(x => x.NameForms is not null);
        RuleFor(x => x.NameForms!.Signature).MaximumLength(256).When(x => x.NameForms is not null);

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
