using System.Globalization;
using Core.Api.Extensions;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.Enums;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using DiplomaControlSystem.Api.Infrastructure.Access;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DiplomaControlSystem.Api.Features.Groups;

public static class GetGroupStatistics
{
    public sealed record StatisticItemDto(
        string Key,
        string Label,
        int Count,
        double Percentage);

    public sealed record StatisticSectionDto(
        string Key,
        string Title,
        IReadOnlyCollection<StatisticItemDto> Items);

    public sealed record PreviousYearStatisticsDto(
        string DefenseYear,
        int GroupsCount,
        int TotalStudents,
        IReadOnlyCollection<StatisticSectionDto> Sections);

    public sealed record GetGroupStatisticsResponse(
        int GroupId,
        string GroupName,
        int TotalStudents,
        IReadOnlyCollection<StatisticSectionDto> Sections,
        PreviousYearStatisticsDto? PreviousYearStatistics);

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
            var secretaryResult = await secretaryAccessService.GetCurrentSecretaryAsync(ct);
            if (secretaryResult.IsFailure)
            {
                return secretaryResult.ErrorDetails;
            }

            var secretary = secretaryResult.Value!;
            var group = await context.Groups
                .AsNoTracking()
                .Where(g => g.Id == groupId)
                .Select(g => new
                {
                    g.Id,
                    g.Name,
                    g.SpecialtyId,
                    g.Year,
                    g.EducationLevel
                })
                .FirstOrDefaultAsync(ct);

            if (group is null)
            {
                return ErrorDetails.NotFound(
                    "Group.NotFound",
                    "Group was not found.");
            }

            if (group.SpecialtyId != secretary.SpecialtyId)
            {
                return ErrorDetails.Forbidden(
                    "Group.Forbidden",
                    "Group does not belong to secretary specialty.");
            }

            var students = await GetStudentStatisticsAsync([groupId], ct);
            var totalStudents = students.Count;
            var sections = BuildSections(students, totalStudents, includePerformanceSection: true);
            var previousYearStatistics = await GetPreviousYearStatisticsAsync(
                group.SpecialtyId,
                group.EducationLevel,
                group.Year,
                ct);

            return new GetGroupStatisticsResponse(
                group.Id,
                group.Name,
                totalStudents,
                sections,
                previousYearStatistics);
        }

        private async Task<PreviousYearStatisticsDto?> GetPreviousYearStatisticsAsync(
            int specialtyId,
            EducationLevel educationLevel,
            string defenseYear,
            CancellationToken ct)
        {
            if (!int.TryParse(defenseYear, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedDefenseYear))
            {
                return null;
            }

            var previousDefenseYear = (parsedDefenseYear - 1).ToString(CultureInfo.InvariantCulture);
            var previousGroupIds = await context.Groups
                .AsNoTracking()
                .Where(group => group.SpecialtyId == specialtyId)
                .Where(group => group.EducationLevel == educationLevel)
                .Where(group => group.Year == previousDefenseYear)
                .Select(group => group.Id)
                .ToListAsync(ct);

            if (previousGroupIds.Count == 0)
            {
                return null;
            }

            var students = await GetStudentStatisticsAsync(previousGroupIds, ct);
            var totalStudents = students.Count;

            return new PreviousYearStatisticsDto(
                previousDefenseYear,
                previousGroupIds.Count,
                totalStudents,
                BuildSections(students, totalStudents, includePerformanceSection: false));
        }

        private async Task<List<StudentStatisticsProjection>> GetStudentStatisticsAsync(
            IReadOnlyCollection<int> groupIds,
            CancellationToken ct)
        {
            return await context.Students
                .AsNoTracking()
                .Where(student => groupIds.Contains(student.GroupId))
                .Select(student => new StudentStatisticsProjection
                {
                    NationalGrade = student.QualificationWork != null
                        ? student.QualificationWork.NationalGrade
                        : NationalGrade.None,
                    HasDiplomaWithHonors = student.QualificationWork != null
                                             && student.QualificationWork.HasDiplomaWithHonors,
                    IsResearchBased = student.QualificationWork != null
                                      && student.QualificationWork.QualificationWorkCharacteristics != null
                                      && student.QualificationWork.QualificationWorkCharacteristics.IsResearchBased,
                    HasRealProjects = student.QualificationWork != null
                                      && student.QualificationWork.QualificationWorkCharacteristics != null
                                      && student.QualificationWork.QualificationWorkCharacteristics.HasRealProjects,
                    IsEcoFriendly = student.QualificationWork != null
                                    && student.QualificationWork.QualificationWorkCharacteristics != null
                                    && student.QualificationWork.QualificationWorkCharacteristics.IsEcoFriendly,
                    IsEnterpriseOrdered = student.QualificationWork != null
                                          && student.QualificationWork.QualificationWorkCharacteristics != null
                                          && student.QualificationWork.QualificationWorkCharacteristics.IsEnterpriseOrdered,
                    IsComplexInteruniversity = student.QualificationWork != null
                                                && student.QualificationWork.QualificationWorkCharacteristics != null
                                                && student.QualificationWork.QualificationWorkCharacteristics.IsComplexInteruniversity,
                    IsComplexInterdepartmental = student.QualificationWork != null
                                                  && student.QualificationWork.QualificationWorkCharacteristics != null
                                                  && student.QualificationWork.QualificationWorkCharacteristics.IsComplexInterdepartmental,
                    IsComplexDepartmental = student.QualificationWork != null
                                            && student.QualificationWork.QualificationWorkCharacteristics != null
                                            && student.QualificationWork.QualificationWorkCharacteristics.IsComplexDepartmental,
                    IsComplexProjectParticipant = student.QualificationWork != null
                                                  && student.QualificationWork.QualificationWorkCharacteristics != null
                                                  && student.QualificationWork.QualificationWorkCharacteristics.IsComplexProjectParticipant,
                    IsRecommendedForMaster = student.QualificationWork != null
                                             && student.QualificationWork.QualificationWorkCharacteristics != null
                                             && student.QualificationWork.QualificationWorkCharacteristics.IsRecommendedForMaster,
                    IsRecommendedForImplementation = student.QualificationWork != null
                                                       && student.QualificationWork.QualificationWorkCharacteristics != null
                                                       && student.QualificationWork.QualificationWorkCharacteristics.IsRecommendedForImplementation,
                    IsDefendedAtEnterprise = student.QualificationWork != null
                                             && student.QualificationWork.QualificationWorkCharacteristics != null
                                             && student.QualificationWork.QualificationWorkCharacteristics.IsDefendedAtEnterprise
                })
                .ToListAsync(ct);
        }

        private static StatisticSectionDto[] BuildSections(
            IReadOnlyCollection<StudentStatisticsProjection> students,
            int totalStudents,
            bool includePerformanceSection)
        {
            var sections = new List<StatisticSectionDto>();

            if (includePerformanceSection)
            {
                sections.Add(BuildPerformanceSection(students, totalStudents));
            }

            sections.AddRange(
                [
                    BuildGradesSection(students, totalStudents),
                    BuildWorkCharacterSection(students, totalStudents),
                    BuildComplexDesignSection(students, totalStudents),
                    BuildAdditionalSection(students, totalStudents)
                ]);

            return sections.ToArray();
        }

        private static StatisticSectionDto BuildPerformanceSection(
            IReadOnlyCollection<StudentStatisticsProjection> students,
            int totalStudents)
        {
            var excellentCount = Count(students, s => s.NationalGrade == NationalGrade.Excellent);
            var goodCount = Count(students, s => s.NationalGrade == NationalGrade.Good);
            var satisfactoryCount = Count(students, s => s.NationalGrade == NationalGrade.Satisfactory);

            return new StatisticSectionDto(
                "performanceIndicators",
                "Показники успішності",
                new[]
                {
                    CreateItem("educationQuality", "Якість навчання", excellentCount + goodCount, totalStudents),
                    CreateItem("overallSuccess", "Загальна успішність", excellentCount + goodCount + satisfactoryCount, totalStudents)
                });
        }

        private static StatisticSectionDto BuildGradesSection(
            IReadOnlyCollection<StudentStatisticsProjection> students,
            int totalStudents)
        {
            return new StatisticSectionDto(
                "gradesAndRecommendations",
                "Оцінки ЕК та рекомендації",
                new[]
                {
                    CreateItem("excellent", "Відмінно", Count(students, s => s.NationalGrade == NationalGrade.Excellent), totalStudents),
                    CreateItem("good", "Добре", Count(students, s => s.NationalGrade == NationalGrade.Good), totalStudents),
                    CreateItem("satisfactory", "Задовільно", Count(students, s => s.NationalGrade == NationalGrade.Satisfactory), totalStudents),
                    CreateItem("diplomaWithHonors", "Диплом з відзнакою", Count(students, s => s.HasDiplomaWithHonors), totalStudents),
                    CreateItem("recommendedForMaster", "Рекомендовано в магістратуру", Count(students, s => s.IsRecommendedForMaster), totalStudents)
                });
        }

        private static StatisticSectionDto BuildWorkCharacterSection(
            IReadOnlyCollection<StudentStatisticsProjection> students,
            int totalStudents)
        {
            return new StatisticSectionDto(
                "workCharacter",
                "Характер виконання дипломних проектів та робіт",
                new[]
                {
                    CreateItem("researchBased", "Дослідного характеру", Count(students, s => s.IsResearchBased), totalStudents),
                    CreateItem("realProjects", "З реальними проектами та конструкторсько-технологічними розробками", Count(students, s => s.HasRealProjects), totalStudents),
                    CreateItem("ecoFriendly", "З раціонального природовикористання, ресурсозбереження та охорони навколишнього середовища", Count(students, s => s.IsEcoFriendly), totalStudents),
                    CreateItem("enterpriseOrdered", "За замовленням підприємства", Count(students, s => s.IsEnterpriseOrdered), totalStudents)
                });
        }

        private static StatisticSectionDto BuildComplexDesignSection(
            IReadOnlyCollection<StudentStatisticsProjection> students,
            int totalStudents)
        {
            return new StatisticSectionDto(
                "complexDiplomaDesign",
                "Комплексне дипломне проектування",
                new[]
                {
                    CreateItem("interuniversity", "Міжвузівські", Count(students, s => s.IsComplexInteruniversity), totalStudents),
                    CreateItem("interdepartmental", "Міжкафедральні", Count(students, s => s.IsComplexInterdepartmental), totalStudents),
                    CreateItem("departmental", "Кафедральні", Count(students, s => s.IsComplexDepartmental), totalStudents),
                    CreateItem("complexProjectParticipant", "Студенти, які брали участь у комплексному проекті", Count(students, s => s.IsComplexProjectParticipant), totalStudents)
                });
        }

        private static StatisticSectionDto BuildAdditionalSection(
            IReadOnlyCollection<StudentStatisticsProjection> students,
            int totalStudents)
        {
            return new StatisticSectionDto(
                "additional",
                "Додатково",
                new[]
                {
                    CreateItem("recommendedForImplementation", "До впровадження", Count(students, s => s.IsRecommendedForImplementation), totalStudents),
                    CreateItem("defendedAtEnterprise", "Захищено на підприємстві", Count(students, s => s.IsDefendedAtEnterprise), totalStudents)
                });
        }

        private static StatisticItemDto CreateItem(
            string key,
            string label,
            int count,
            int totalStudents)
        {
            return new StatisticItemDto(key, label, count, CalculatePercentage(count, totalStudents));
        }

        private static int Count(
            IEnumerable<StudentStatisticsProjection> students,
            Func<StudentStatisticsProjection, bool> predicate)
        {
            return students.Count(predicate);
        }

        private static double CalculatePercentage(int count, int totalStudents)
        {
            if (totalStudents == 0)
            {
                return 0;
            }

            return Math.Round((double)count * 100 / totalStudents, 1, MidpointRounding.AwayFromZero);
        }

        private sealed class StudentStatisticsProjection
        {
            public NationalGrade NationalGrade { get; init; }
            public bool HasDiplomaWithHonors { get; init; }
            public bool IsResearchBased { get; init; }
            public bool HasRealProjects { get; init; }
            public bool IsEcoFriendly { get; init; }
            public bool IsEnterpriseOrdered { get; init; }
            public bool IsComplexInteruniversity { get; init; }
            public bool IsComplexInterdepartmental { get; init; }
            public bool IsComplexDepartmental { get; init; }
            public bool IsComplexProjectParticipant { get; init; }
            public bool IsRecommendedForMaster { get; init; }
            public bool IsRecommendedForImplementation { get; init; }
            public bool IsDefendedAtEnterprise { get; init; }
        }
    }
}
