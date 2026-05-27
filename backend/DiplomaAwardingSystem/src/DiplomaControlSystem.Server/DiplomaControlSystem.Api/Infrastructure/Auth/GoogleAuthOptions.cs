namespace DiplomaControlSystem.Api.Infrastructure.Auth;

internal sealed class GoogleAuthOptions
{
    public const string SectionName = "Authentication:Google";

    public string ClientId { get; init; } = string.Empty;
    public string MetadataAddress { get; init; } = "https://accounts.google.com/.well-known/openid-configuration";
}
