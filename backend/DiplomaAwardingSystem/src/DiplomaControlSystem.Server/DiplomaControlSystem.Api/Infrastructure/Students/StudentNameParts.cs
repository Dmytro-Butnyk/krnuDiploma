namespace DiplomaControlSystem.Api.Infrastructure.Students;

internal sealed record StudentNameParts(string LastName, string FirstName, string MiddleName)
{
    public string FullName => string.Join(' ', LastName, FirstName, MiddleName);
}
