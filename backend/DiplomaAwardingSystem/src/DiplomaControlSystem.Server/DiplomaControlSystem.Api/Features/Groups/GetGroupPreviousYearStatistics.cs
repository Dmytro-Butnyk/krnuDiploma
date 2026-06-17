using System.Globalization;
using Core.Api.Extensions;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using DiplomaControlSystem.Api.Infrastructure.Access;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DiplomaControlSystem.Api.Features.Groups;

public static class GetGroupPreviousYearStatistics
{
    public sealed record GetGroupPreviousYearStatisticsResponse(
        int GroupId,
        string GroupName,
        GroupStatisticsShared.SnapshotDto CurrentGroup,
        GroupStatisticsShared.SnapshotDto? PreviousYear);

    internal static class Endpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/groups/{groupId:int}/statistics/previous-year-comparison", Handle)
                .WithSummary("Gets group statistics compared with the previous defence year")
                .Produces<GetGroupPreviousYearStatisticsResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithTags("Groups");
        }

        private static async Task<Results<Ok<GetGroupPreviousYearStatisticsResponse>, ProblemHttpResult>> Handle(
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
        public async Task<Result<GetGroupPreviousYearStatisticsResponse>> HandleAsync(
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
            var currentStudents = await GroupStatisticsShared.GetStudentStatisticsAsync(context, [group.Id], ct);
            var currentSnapshot = CreateSnapshot(
                group.Year,
                groupsCount: 1,
                currentStudents);

            var previousSnapshot = await GetPreviousYearSnapshotAsync(group, ct);

            return new GetGroupPreviousYearStatisticsResponse(
                group.Id,
                group.Name,
                currentSnapshot,
                previousSnapshot);
        }

        private async Task<GroupStatisticsShared.SnapshotDto?> GetPreviousYearSnapshotAsync(
            GroupStatisticsShared.GroupProjection group,
            CancellationToken ct)
        {
            if (!int.TryParse(group.Year, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedDefenseYear))
            {
                return null;
            }

            var previousDefenseYear = (parsedDefenseYear - 1).ToString(CultureInfo.InvariantCulture);
            var previousGroupIds = await context.Groups
                .AsNoTracking()
                .Where(previousGroup => previousGroup.SpecialtyId == group.SpecialtyId)
                .Where(previousGroup => previousGroup.EducationLevel == group.EducationLevel)
                .Where(previousGroup => previousGroup.Year == previousDefenseYear)
                .Select(previousGroup => previousGroup.Id)
                .ToListAsync(ct);

            if (previousGroupIds.Count == 0)
            {
                return null;
            }

            var previousStudents = await GroupStatisticsShared.GetStudentStatisticsAsync(context, previousGroupIds, ct);

            return CreateSnapshot(
                previousDefenseYear,
                previousGroupIds.Count,
                previousStudents);
        }

        private static GroupStatisticsShared.SnapshotDto CreateSnapshot(
            string defenseYear,
            int groupsCount,
            List<GroupStatisticsShared.StudentProjection> students)
        {
            var totalStudents = students.Count;

            return new GroupStatisticsShared.SnapshotDto(
                defenseYear,
                groupsCount,
                totalStudents,
                GroupStatisticsShared.BuildSections(students, totalStudents));
        }
    }
}
