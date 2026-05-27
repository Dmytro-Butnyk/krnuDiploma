using Core.Domain.DependencyInjectionInterfaces;
using Core.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DiplomaControlSystem.Api.Infrastructure.DiplomaExaminationCommissions;

internal sealed class DiplomaExaminationCommissionCleanupService(DbDocGenContext context) : IScopedService
{
    public async Task RemoveEmptyCommissionsAsync(IEnumerable<int?> commissionIds, CancellationToken ct)
    {
        var ids = commissionIds
            .Where(id => id is not null)
            .Select(id => id!.Value)
            .Distinct()
            .ToArray();

        foreach (var commissionId in ids)
        {
            var hasGroups = await context.Groups
                .AsNoTracking()
                .AnyAsync(group => group.DiplomaExaminationCommissionId == commissionId, ct);

            if (hasGroups)
            {
                continue;
            }

            var commission = await context.DiplomaExaminationCommissions
                .FirstOrDefaultAsync(dec => dec.Id == commissionId, ct);

            if (commission is null)
            {
                continue;
            }

            await context.Archives
                .Where(archive => archive.DiplomaExaminationCommissionId == commissionId)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        archive => archive.DiplomaExaminationCommissionId,
                        (int?)null),
                    ct);

            context.DiplomaExaminationCommissions.Remove(commission);
        }
    }
}
