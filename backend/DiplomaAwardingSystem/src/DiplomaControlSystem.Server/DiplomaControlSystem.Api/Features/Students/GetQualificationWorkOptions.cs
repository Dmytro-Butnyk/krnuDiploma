using Core.Api.Extensions;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using DiplomaControlSystem.Api.Infrastructure.Access;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DiplomaControlSystem.Api.Features.Students;

public static class GetQualificationWorkOptions
{
    public sealed record TeacherOptionDto(int Id, string FullName, string ShortName);

    public sealed record GetQualificationWorkOptionsResponse(
        IReadOnlyCollection<TeacherOptionDto> Supervisors,
        IReadOnlyCollection<TeacherOptionDto> Reviewers);

    internal static class Endpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/students/{studentId:int}/qualification-work-options", Handle)
                .WithSummary("Gets supervisor and reviewer options for student qualification work")
                .Produces<GetQualificationWorkOptionsResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithTags("Students");
        }

        private static async Task<Results<Ok<GetQualificationWorkOptionsResponse>, ProblemHttpResult>> Handle(
            [FromRoute] int studentId,
            [FromQuery] string secretaryEmail,
            [FromServices] Handler handler,
            CancellationToken ct)
        {
            var result = await handler.HandleAsync(studentId, secretaryEmail, ct);

            if (result.IsFailure)
            {
                return result.ToProblemDetails();
            }

            return TypedResults.Ok(result.Value!);
        }
    }

    private sealed class Handler(
        DbDocGenContext context,
        StudentAccessService studentAccessService) : IScopedService
    {
        public async Task<Result<GetQualificationWorkOptionsResponse>> HandleAsync(
            int studentId,
            string secretaryEmail,
            CancellationToken ct)
        {
            var accessResult = await studentAccessService.GetForSecretaryAsync(studentId, secretaryEmail, ct);
            if (accessResult.IsFailure)
            {
                return accessResult.ErrorDetails;
            }

            var access = accessResult.Value!;
            var supervisors = await context.Teachers
                .AsNoTracking()
                .Where(t => t.SpecialtyId == access.SpecialtyId)
                .OrderBy(t => t.ShortName)
                .Select(t => new TeacherOptionDto(t.Id, t.FullName, t.ShortName))
                .ToListAsync(ct);

            var reviewers = await context.Teachers
                .AsNoTracking()
                .Where(t => t.SpecialtyId != access.SpecialtyId)
                .OrderBy(t => t.ShortName)
                .Select(t => new TeacherOptionDto(t.Id, t.FullName, t.ShortName))
                .ToListAsync(ct);

            return new GetQualificationWorkOptionsResponse(supervisors, reviewers);
        }
    }
}
