using Core.Api.Extensions;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using DiplomaControlSystem.Api.Infrastructure.Access;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace DiplomaControlSystem.Api.Features.Groups;

public static class GetGroupPracticeBaseStatistics
{
    public sealed record PracticeBaseRatingItemDto(
        string Key,
        int? Rank,
        string? PracticeBase,
        int StudentsCount);

    public sealed record GetGroupPracticeBaseStatisticsResponse(
        int GroupId,
        string GroupName,
        int TotalStudents,
        int TotalPracticeBases,
        IReadOnlyCollection<PracticeBaseRatingItemDto> Items);

    internal static class Endpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/groups/{groupId:int}/statistics/practice-bases", Handle)
                .WithSummary("Gets practice base rating statistics for a group")
                .Produces<GetGroupPracticeBaseStatisticsResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithTags("Groups");
        }

        private static async Task<Results<Ok<GetGroupPracticeBaseStatisticsResponse>, ProblemHttpResult>> Handle(
            [FromRoute] int groupId,
            [FromServices] Handler handler,
            CancellationToken ct)
        {
            var result = await handler.HandleAsync(groupId, ct);

            if (result.IsFailure)
            {
                return result.ToProblemDetails();
            }

            return TypedResults.Ok(result.Value!);
        }
    }

    private sealed class Handler(
        DbDocGenContext context,
        SecretaryAccessService secretaryAccessService) : IScopedService
    {
        public async Task<Result<GetGroupPracticeBaseStatisticsResponse>> HandleAsync(
            int groupId,
            CancellationToken ct)
        {
            var groupResult = await GroupStatisticsShared.GetAccessibleGroupAsync(
                context,
                secretaryAccessService,
                groupId,
                ct);

            if (groupResult.IsFailure)
            {
                return groupResult.ErrorDetails;
            }

            var group = groupResult.Value!;
            var students = await GroupStatisticsShared.GetStudentStatisticsAsync(context, [group.Id], ct);
            var practiceBaseRows = students
                .Select(student => student.PracticeBase.Trim())
                .Where(practiceBase => !string.IsNullOrWhiteSpace(practiceBase))
                .GroupBy(practiceBase => practiceBase)
                .Select(practiceBaseGroup => new
                {
                    PracticeBase = practiceBaseGroup.Key,
                    StudentsCount = practiceBaseGroup.Count()
                })
                .OrderByDescending(item => item.StudentsCount)
                .ThenBy(item => item.PracticeBase)
                .Select((item, index) => new PracticeBaseRatingItemDto(
                    "practiceBase",
                    index + 1,
                    item.PracticeBase,
                    item.StudentsCount))
                .ToList();

            var withoutPracticeBaseCount = students.Count(student => string.IsNullOrWhiteSpace(student.PracticeBase));
            if (withoutPracticeBaseCount > 0)
            {
                practiceBaseRows.Add(new PracticeBaseRatingItemDto(
                    "withoutPracticeBase",
                    null,
                    null,
                    withoutPracticeBaseCount));
            }

            return new GetGroupPracticeBaseStatisticsResponse(
                group.Id,
                group.Name,
                students.Count,
                practiceBaseRows.Count(item => item.Key == "practiceBase"),
                practiceBaseRows);
        }
    }
}
