namespace Core.Domain.Entities;

public sealed class PersonNameForms
{
    public string Nominative { get; set; }
    public string Genitive { get; set; }
    public string Dative { get; set; }
    public string Signature { get; set; }

    private PersonNameForms()
    {
        Nominative = string.Empty;
        Genitive = string.Empty;
        Dative = string.Empty;
        Signature = string.Empty;
    }

    public PersonNameForms(string nominative, string genitive, string dative, string signature)
    {
        Nominative = nominative;
        Genitive = genitive;
        Dative = dative;
        Signature = signature;
    }

    public static PersonNameForms FromDefault(string fullName, string? signature = null)
    {
        var normalizedFullName = fullName.Trim();
        var normalizedSignature = string.IsNullOrWhiteSpace(signature)
            ? normalizedFullName
            : signature.Trim();

        return new PersonNameForms(
            normalizedFullName,
            normalizedFullName,
            normalizedFullName,
            normalizedSignature);
    }
}
