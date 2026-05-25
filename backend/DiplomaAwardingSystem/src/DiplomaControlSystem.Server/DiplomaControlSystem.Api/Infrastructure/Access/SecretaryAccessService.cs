using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DiplomaControlSystem.Api.Infrastructure.Access;

internal sealed class SecretaryAccessService(DbDocGenContext context) : IScopedService
{
    public async Task<Result<SecretaryAccessContext>> GetActiveSecretaryAsync(
        string email,
        CancellationToken ct)
    {
        var secretary = await context.Secretaries
            .AsNoTracking()
            .Where(s => EF.Functions.ILike(s.Email, email.Trim()))
            .Select(s => new SecretaryAccessContext(
                s.Id,
                s.FullName,
                s.SpecialtyId,
                s.Specialty!.Name,
                s.IsActive))
            .FirstOrDefaultAsync(ct);

        if (secretary is null)
        {
            return ErrorDetails.NotFound(
                "Secretary.NotFound",
                "Secretary with the specified email was not found.");
        }

        if (!secretary.IsActive)
        {
            return ErrorDetails.Forbidden(
                "Secretary.Inactive",
                "Secretary with the specified email is inactive.");
        }

        return secretary;
    }
}
