using Core.Api.Extensions;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using DiplomaControlSystem.Api.Infrastructure.Access;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DiplomaControlSystem.Api.Features.Students;

public static class DeleteStudent
{
    internal static class Endpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("/students/{studentId:int}", Handle)
                .WithSummary("Deletes a student and all related default diploma data")
                .Produces(StatusCodes.Status204NoContent)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithTags("Students");
        }

        private static async Task<Results<NoContent, ProblemHttpResult>> Handle(
            [FromRoute] int studentId,
            [FromServices] Handler handler,
            CancellationToken ct)
        {
            var result = await handler.HandleAsync(studentId, ct);

            if (result.IsFailure)
            {
                return result.ToProblemDetails();
            }

            return TypedResults.NoContent();
        }
    }

    private sealed class Handler(
        DbDocGenContext context,
        SecretaryAccessService secretaryAccessService) : IScopedService
    {
        public async Task<Result> HandleAsync(
            int studentId,
            CancellationToken ct)
        {
            var secretaryResult = await secretaryAccessService.GetCurrentSecretaryAsync(ct);
            if (secretaryResult.IsFailure)
            {
                return secretaryResult.ErrorDetails;
            }

            var secretary = secretaryResult.Value!;
            var student = await context.Students
                .Include(s => s.Group)
                .FirstOrDefaultAsync(s => s.Id == studentId, ct);

            if (student is null)
            {
                return ErrorDetails.NotFound(
                    "Student.NotFound",
                    "Student was not found.");
            }

            if (student.Group?.SpecialtyId != secretary.SpecialtyId)
            {
                return ErrorDetails.Forbidden(
                    "Student.Forbidden",
                    "Student does not belong to secretary specialty.");
            }

            context.Students.Remove(student);
            await context.SaveChangesAsync(ct);

            return Result.Success();
        }
    }
}
