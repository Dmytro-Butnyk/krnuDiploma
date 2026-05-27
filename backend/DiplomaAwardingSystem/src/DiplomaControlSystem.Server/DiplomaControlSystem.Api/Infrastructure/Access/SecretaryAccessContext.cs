namespace DiplomaControlSystem.Api.Infrastructure.Access;

internal sealed record SecretaryAccessContext(
    int SecretaryId,
    string Email,
    string FullName,
    int SpecialtyId,
    string SpecialtyName,
    bool IsActive,
    bool IsSuperSecretary);
