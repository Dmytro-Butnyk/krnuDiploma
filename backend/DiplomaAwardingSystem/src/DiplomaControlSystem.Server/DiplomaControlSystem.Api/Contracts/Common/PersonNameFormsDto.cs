using Core.Domain.Entities;

namespace DiplomaControlSystem.Api.Contracts.Common;

public sealed record PersonNameFormsDto(
    string Nominative,
    string Genitive,
    string Dative,
    string Signature)
{
    public static PersonNameFormsDto From(PersonNameForms forms)
    {
        return new PersonNameFormsDto(
            forms.Nominative,
            forms.Genitive,
            forms.Dative,
            forms.Signature);
    }

    public PersonNameForms ToDomain(string fallbackFullName, string? fallbackSignature = null)
    {
        var defaultForms = PersonNameForms.FromDefault(fallbackFullName, fallbackSignature);

        return new PersonNameForms(
            string.IsNullOrWhiteSpace(Nominative) ? defaultForms.Nominative : Nominative.Trim(),
            string.IsNullOrWhiteSpace(Genitive) ? defaultForms.Genitive : Genitive.Trim(),
            string.IsNullOrWhiteSpace(Dative) ? defaultForms.Dative : Dative.Trim(),
            string.IsNullOrWhiteSpace(Signature) ? defaultForms.Signature : Signature.Trim());
    }
}
