using Core.Api.Extensions;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using DiplomaControlSystem.Api.Infrastructure.Access;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace DiplomaControlSystem.Api.Features.Groups;

public static class GetGroupStatistics
{
    public sealed record GetGroupStatisticsResponse(
        int GroupId,
        string GroupName,
        int TotalStudents,
        IReadOnlyCollection<GroupStatisticsShared.StatisticSectionDto> Sections);

    internal static class Endpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/groups/{groupId:int}/statistics", Handle)
                .WithSummary("Gets defence result statistics for a group")
                .Produces<GetGroupStatisticsResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithTags("Groups");
        }

        private static async Task<Results<Ok<GetGroupStatisticsResponse>, ProblemHttpResult>> Handle(
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
        public async Task<Result<GetGroupStatisticsResponse>> HandleAsync(
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
            var totalStudents = students.Count;

            return new GetGroupStatisticsResponse(
                group.Id,
                group.Name,
                totalStudents,
                GroupStatisticsShared.BuildSections(students, totalStudents));
        }
    }
}
