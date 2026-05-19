namespace DiplomaControlSystem.Api.Infrastructure.Access;

internal sealed record SecretaryAccessContext(int SecretaryId, string FullName, int SpecialtyId, bool IsActive);
