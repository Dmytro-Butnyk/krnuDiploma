namespace DiplomaControlSystem.Api.Infrastructure.Access;

internal sealed record StudentAccessContext(
    int StudentId,
    int GroupId,
    int SpecialtyId);
