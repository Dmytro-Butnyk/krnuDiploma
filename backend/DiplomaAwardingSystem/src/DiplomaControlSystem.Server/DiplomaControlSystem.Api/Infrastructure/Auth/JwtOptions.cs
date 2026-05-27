namespace DiplomaControlSystem.Api.Infrastructure.Auth;

internal sealed class JwtOptions
{
    public const string SectionName = "Authentication:Jwt";

    public string Issuer { get; init; } = "DiplomaControlSystem";
    public string Audience { get; init; } = "DiplomaControlSystem.Client";
    public string SigningKey { get; init; } = string.Empty;
    public int AccessTokenMinutes { get; init; } = 60;
}
