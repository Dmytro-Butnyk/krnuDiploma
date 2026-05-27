using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.ResultPattern;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace DiplomaControlSystem.Api.Infrastructure.Auth;

internal sealed class GoogleIdTokenValidator : ISingletonService
{
    private static readonly string[] ValidIssuers =
    [
        "https://accounts.google.com",
        "accounts.google.com"
    ];

    private readonly GoogleAuthOptions options;
    private readonly ConfigurationManager<OpenIdConnectConfiguration> configurationManager;
    private readonly JwtSecurityTokenHandler tokenHandler = new();

    public GoogleIdTokenValidator(IOptions<GoogleAuthOptions> options)
    {
        this.options = options.Value;
        configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            this.options.MetadataAddress,
            new OpenIdConnectConfigurationRetriever());
    }

    public async Task<Result<GoogleUserInfo>> ValidateAsync(string idToken, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(options.ClientId))
        {
            return ErrorDetails.Failure(
                "Auth.GoogleClientIdMissing",
                "Google client id is not configured.");
        }

        if (string.IsNullOrWhiteSpace(idToken))
        {
            return ErrorDetails.Unauthorized(
                "Auth.GoogleTokenMissing",
                "Google id token is required.");
        }

        try
        {
            var configuration = await configurationManager.GetConfigurationAsync(ct);
            var validationResult = await tokenHandler.ValidateTokenAsync(
                idToken,
                new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuers = ValidIssuers,
                    ValidateAudience = true,
                    ValidAudience = options.ClientId,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKeys = configuration.SigningKeys,
                    ClockSkew = TimeSpan.FromMinutes(2)
                });

            if (!validationResult.IsValid || validationResult.ClaimsIdentity is null)
            {
                return ErrorDetails.Unauthorized(
                    "Auth.GoogleTokenInvalid",
                    "Google id token is invalid.");
            }

            var principal = new ClaimsPrincipal(validationResult.ClaimsIdentity);

            var emailVerified = principal.FindFirst("email_verified")?.Value;
            if (!string.Equals(emailVerified, "true", StringComparison.OrdinalIgnoreCase))
            {
                return ErrorDetails.Unauthorized(
                    "Auth.EmailNotVerified",
                    "Google account email is not verified.");
            }

            var subject = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? principal.FindFirst("sub")?.Value;
            var email = principal.FindFirst(ClaimTypes.Email)?.Value
                        ?? principal.FindFirst("email")?.Value;
            var fullName = principal.FindFirst(ClaimTypes.Name)?.Value
                           ?? principal.FindFirst("name")?.Value
                           ?? string.Empty;

            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(email))
            {
                return ErrorDetails.Unauthorized(
                    "Auth.GoogleTokenInvalid",
                    "Google id token does not contain required user claims.");
            }

            return new GoogleUserInfo(subject, email.Trim(), fullName.Trim());
        }
        catch (SecurityTokenException)
        {
            return ErrorDetails.Unauthorized(
                "Auth.GoogleTokenInvalid",
                "Google id token is invalid.");
        }
        catch (InvalidOperationException)
        {
            return ErrorDetails.Unauthorized(
                "Auth.GoogleTokenInvalid",
                "Google id token is invalid.");
        }
    }
}
