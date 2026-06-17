using Core.Api.Extensions;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using DiplomaControlSystem.Api.Infrastructure.Access;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace DiplomaControlSystem.Api.Features.Groups;

public static class GetGroupSupervisorWorkloadStatistics
{
    public sealed record SupervisorWorkloadRowDto(
        string Key,
        int? TeacherId,
        string? FullName,
        string? ShortName,
        int StudentsCount,
        double? AverageScore,
        int DiplomasWithHonorsCount,
        double? AveragePlagiarismPercent);

    public sealed record SupervisorWorkloadSummaryDto(
        int TotalSupervisors,
        int TotalStudents);

    public sealed record GetGroupSupervisorWorkloadStatisticsResponse(
        int GroupId,
        string GroupName,
        SupervisorWorkloadSummaryDto Summary,
        IReadOnlyCollection<SupervisorWorkloadRowDto> Items);

    internal static class Endpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/groups/{groupId:int}/statistics/supervisor-workload", Handle)
                .WithSummary("Gets supervisor workload statistics for a group")
                .Produces<GetGroupSupervisorWorkloadStatisticsResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithTags("Groups");
        }

        private static async Task<Results<Ok<GetGroupSupervisorWorkloadStatisticsResponse>, ProblemHttpResult>> Handle(
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
        public async Task<Result<GetGroupSupervisorWorkloadStatisticsResponse>> HandleAsync(
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
            var supervisorRows = students
                .Where(student => student.SupervisorId is not null)
                .GroupBy(student => new
                {
                    Id = student.SupervisorId!.Value,
                    FullName = student.SupervisorFullName ?? string.Empty,
                    ShortName = student.SupervisorShortName ?? string.Empty
                })
                .Select(supervisorGroup => new SupervisorWorkloadRowDto(
                    "supervisor",
                    supervisorGroup.Key.Id,
                    supervisorGroup.Key.FullName,
                    supervisorGroup.Key.ShortName,
                    supervisorGroup.Count(),
                    GroupStatisticsShared.RoundAverage(supervisorGroup.Average(student => student.CommissionScore)),
                    supervisorGroup.Count(student => student.HasDiplomaWithHonors),
                    GroupStatisticsShared.RoundAverage(supervisorGroup.Average(student => student.PlagiarismPercent))))
                .OrderByDescending(row => row.StudentsCount)
                .ThenBy(row => row.FullName)
                .ToList();

            var withoutSupervisorCount = students.Count(student => student.SupervisorId is null);
            if (withoutSupervisorCount > 0)
            {
                supervisorRows.Add(new SupervisorWorkloadRowDto(
                    "withoutSupervisor",
                    null,
                    null,
                    null,
                    withoutSupervisorCount,
                    null,
                    0,
                    null));
            }

            return new GetGroupSupervisorWorkloadStatisticsResponse(
                group.Id,
                group.Name,
                new SupervisorWorkloadSummaryDto(
                    supervisorRows.Count(row => row.TeacherId is not null),
                    students.Count),
                supervisorRows);
        }
    }
}
