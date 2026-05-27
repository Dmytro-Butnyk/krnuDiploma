using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DiplomaControlSystem.Api.Infrastructure.Access;

internal sealed class StudentAccessService(
    DbDocGenContext context,
    SecretaryAccessService secretaryAccessService) : IScopedService
{
    public async Task<Result<StudentAccessContext>> GetForCurrentSecretaryAsync(
        int studentId,
        CancellationToken ct)
    {
        var secretaryResult = await secretaryAccessService.GetCurrentSecretaryAsync(ct);
        if (secretaryResult.IsFailure)
        {
            return secretaryResult.ErrorDetails;
        }

        var student = await context.Students
            .AsNoTracking()
            .Where(s => s.Id == studentId)
            .Select(s => new
            {
                s.Id,
                s.GroupId,
                s.Group!.SpecialtyId
            })
            .FirstOrDefaultAsync(ct);

        if (student is null)
        {
            return ErrorDetails.NotFound(
                "Student.NotFound",
                "Student was not found.");
        }

        var secretary = secretaryResult.Value!;
        if (student.SpecialtyId != secretary.SpecialtyId)
        {
            return ErrorDetails.Forbidden(
                "Student.Forbidden",
                "Student does not belong to secretary specialty.");
        }

        return new StudentAccessContext(
            student.Id,
            student.GroupId,
            student.SpecialtyId);
    }
}
