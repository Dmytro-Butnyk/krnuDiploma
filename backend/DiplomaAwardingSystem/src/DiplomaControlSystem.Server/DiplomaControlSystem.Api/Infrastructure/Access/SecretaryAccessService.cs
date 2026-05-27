using System.Globalization;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using DiplomaControlSystem.Api.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;

namespace DiplomaControlSystem.Api.Infrastructure.Access;

internal sealed class SecretaryAccessService(
    DbDocGenContext context,
    IHttpContextAccessor httpContextAccessor) : IScopedService
{
    public async Task<Result<SecretaryAccessContext>> GetCurrentSecretaryAsync(CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext?.User;
        var secretaryIdClaim = user?.FindFirst(AuthClaims.SecretaryId)?.Value
                              ?? user?.FindFirst("sub")?.Value;

        if (!int.TryParse(secretaryIdClaim, NumberStyles.Integer, CultureInfo.InvariantCulture, out var secretaryId))
        {
            return ErrorDetails.Unauthorized(
                "Auth.UserMissing",
                "Authenticated secretary id was not found.");
        }

        return await GetActiveSecretaryByIdAsync(secretaryId, ct);
    }

    public async Task<Result<SecretaryAccessContext>> GetCurrentSuperSecretaryAsync(CancellationToken ct)
    {
        var secretaryResult = await GetCurrentSecretaryAsync(ct);
        if (secretaryResult.IsFailure)
        {
            return secretaryResult.ErrorDetails;
        }

        if (!secretaryResult.Value!.IsSuperSecretary)
        {
            return ErrorDetails.Forbidden(
                "Secretary.SuperRoleRequired",
                "Super secretary role is required.");
        }

        return secretaryResult;
    }

    private async Task<Result<SecretaryAccessContext>> GetActiveSecretaryByIdAsync(
        int secretaryId,
        CancellationToken ct)
    {
        var secretary = await context.Secretaries
            .AsNoTracking()
            .Where(s => s.Id == secretaryId)
            .Select(s => new SecretaryAccessContext(
                s.Id,
                s.Email,
                s.FullName,
                s.SpecialtyId,
                s.Specialty!.Name,
                s.IsActive,
                s.IsSuperSecretary))
            .FirstOrDefaultAsync(ct);

        if (secretary is null)
        {
            return ErrorDetails.Unauthorized(
                "Secretary.NotFound",
                "Authenticated secretary was not found.");
        }

        if (!secretary.IsActive)
        {
            return ErrorDetails.Forbidden(
                "Secretary.Inactive",
                "Secretary account is inactive.");
        }

        var specialtyIsActive = await context.Specialties
            .AsNoTracking()
            .Where(s => s.Id == secretary.SpecialtyId)
            .Select(s => s.IsActive)
            .FirstOrDefaultAsync(ct);

        if (!specialtyIsActive)
        {
            return ErrorDetails.Forbidden(
                "Specialty.Inactive",
                "Secretary specialty is inactive.");
        }

        return secretary;
    }
}
