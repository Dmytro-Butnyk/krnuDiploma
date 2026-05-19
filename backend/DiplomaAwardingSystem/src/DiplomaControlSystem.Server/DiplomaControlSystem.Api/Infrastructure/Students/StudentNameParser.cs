namespace DiplomaControlSystem.Api.Infrastructure.Students;

internal static class StudentNameParser
{
    public static StudentNameParts Parse(string fullName)
    {
        var parts = fullName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return parts.Length switch
        {
            0 => new StudentNameParts(string.Empty, string.Empty, string.Empty),
            1 => new StudentNameParts(parts[0], string.Empty, string.Empty),
            2 => new StudentNameParts(parts[0], parts[1], string.Empty),
            _ => new StudentNameParts(parts[0], parts[1], string.Join(' ', parts[2..]))
        };
    }

    public static StudentNameParts Build(string lastName, string firstName, string middleName)
    {
        return new StudentNameParts(
            lastName.Trim(),
            firstName.Trim(),
            middleName.Trim());
    }
}
