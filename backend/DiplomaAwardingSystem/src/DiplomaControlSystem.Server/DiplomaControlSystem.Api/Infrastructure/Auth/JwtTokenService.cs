using System.IdentityModel.Tokens.Jwt;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.Entities.StudyGroup;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DiplomaControlSystem.Api.Infrastructure.Auth;

internal sealed class JwtTokenService(IOptions<JwtOptions> options) : ISingletonService
{
    private readonly JwtOptions options = options.Value;

    public string CreateAccessToken(Secretary secretary)
    {
        var now = DateTime.UtcNow;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, secretary.Id.ToString(CultureInfo.InvariantCulture)),
            new(JwtRegisteredClaimNames.Email, secretary.Email),
            new(JwtRegisteredClaimNames.Name, secretary.FullName),
            new(AuthClaims.SecretaryId, secretary.Id.ToString(CultureInfo.InvariantCulture)),
            new(AuthClaims.SpecialtyId, secretary.SpecialtyId.ToString(CultureInfo.InvariantCulture)),
            new(ClaimTypes.Role, AuthRoles.Secretary)
        };

        if (secretary.IsSuperSecretary)
        {
            claims.Add(new Claim(ClaimTypes.Role, AuthRoles.SuperSecretary));
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(options.AccessTokenMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
