namespace DiplomaControlSystem.Api.Infrastructure.Auth;

internal sealed record GoogleUserInfo(
    string Subject,
    string Email,
    string FullName);
