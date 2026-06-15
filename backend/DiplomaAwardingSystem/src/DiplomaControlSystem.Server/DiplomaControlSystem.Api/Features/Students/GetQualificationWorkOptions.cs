using Core.Api.Extensions;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using DiplomaControlSystem.Api.Contracts.Common;
using DiplomaControlSystem.Api.Infrastructure.Access;
using DiplomaControlSystem.Api.Infrastructure.Students;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DiplomaControlSystem.Api.Features.Students;

public static class GetQualificationWorkOptions
{
    public sealed record TeacherOptionDto(int Id, string FullName, string ShortName);

    public sealed record GetQualificationWorkOptionsResponse(
        IReadOnlyCollection<TeacherOptionDto> Teachers,
        IReadOnlyCollection<TeacherOptionDto> Supervisors,
        IReadOnlyCollection<TeacherOptionDto> Reviewers,
        IReadOnlyCollection<DefenceQuestionAuthorOptionDto> DefenceQuestionAuthors);

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
            [FromServices] Handler handler,
            CancellationToken ct)
        {
            var result = await handler.HandleAsync(studentId, ct);

            if (result.IsFailure)
            {
                return result.ToProblemDetails();
            }

            return TypedResults.Ok(result.Value!);
        }
    }

    private sealed class Handler(
        DbDocGenContext context,
        StudentAccessService studentAccessService,
        DefenceQuestionAuthorOptionsProvider defenceQuestionAuthorOptionsProvider) : IScopedService
    {
        public async Task<Result<GetQualificationWorkOptionsResponse>> HandleAsync(
            int studentId,
            CancellationToken ct)
        {
            var accessResult = await studentAccessService.GetForCurrentSecretaryAsync(studentId, ct);
            if (accessResult.IsFailure)
            {
                return accessResult.ErrorDetails;
            }

            var teachers = await context.Teachers
                .AsNoTracking()
                .Where(t => t.IsActive)
                .OrderBy(t => t.ShortName)
                .Select(t => new TeacherOptionDto(t.Id, t.FullName, t.ShortName))
                .ToListAsync(ct);

            var defenceQuestionAuthors = await defenceQuestionAuthorOptionsProvider.GetByStudentIdAsync(studentId, ct);

            return new GetQualificationWorkOptionsResponse(
                teachers,
                teachers,
                teachers,
                defenceQuestionAuthors);
        }
    }
}
