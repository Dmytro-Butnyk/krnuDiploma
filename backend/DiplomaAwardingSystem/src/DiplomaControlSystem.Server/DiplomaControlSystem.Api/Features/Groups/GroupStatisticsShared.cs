using Core.Domain.Enums;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using DiplomaControlSystem.Api.Infrastructure.Access;
using Microsoft.EntityFrameworkCore;

namespace DiplomaControlSystem.Api.Features.Groups;

public static class GroupStatisticsShared
{
    public sealed record StatisticItemDto(
        string Key,
        int Count,
        double Percentage);

    public sealed record StatisticSectionDto(
        string Key,
        IReadOnlyCollection<StatisticItemDto> Items);

    public sealed record SnapshotDto(
        string DefenseYear,
        int GroupsCount,
        int TotalStudents,
        IReadOnlyCollection<StatisticSectionDto> Sections);

    public sealed record GroupProjection(
        int Id,
        string Name,
        int SpecialtyId,
        string Year,
        EducationLevel EducationLevel);

    public sealed class StudentProjection
    {
        public NationalGrade NationalGrade { get; init; }

        public int CommissionScore { get; init; }

        public float PlagiarismPercent { get; init; }

        public bool HasDiplomaWithHonors { get; init; }

        public string PracticeBase { get; init; } = string.Empty;

        public int? SupervisorId { get; init; }

        public string? SupervisorFullName { get; init; }

        public string? SupervisorShortName { get; init; }

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

    internal static async Task<Result<GroupProjection>> GetAccessibleGroupAsync(
        DbDocGenContext context,
        SecretaryAccessService secretaryAccessService,
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
            .Select(g => new GroupProjection(
                g.Id,
                g.Name,
                g.SpecialtyId,
                g.Year,
                g.EducationLevel))
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

        return group;
    }

    internal static async Task<List<StudentProjection>> GetStudentStatisticsAsync(
        DbDocGenContext context,
        IReadOnlyCollection<int> groupIds,
        CancellationToken ct)
    {
        return await context.Students
            .AsNoTracking()
            .Where(student => groupIds.Contains(student.GroupId))
            .Select(student => new StudentProjection
            {
                NationalGrade = student.QualificationWork != null
                    ? student.QualificationWork.NationalGrade
                    : NationalGrade.None,
                CommissionScore = student.QualificationWork != null
                    ? student.QualificationWork.CommissionScore
                    : 0,
                PlagiarismPercent = student.QualificationWork != null
                    ? student.QualificationWork.PlagiarismPercent
                    : 0,
                HasDiplomaWithHonors = student.QualificationWork != null
                                         && student.QualificationWork.HasDiplomaWithHonors,
                PracticeBase = student.QualificationWork != null
                    ? student.QualificationWork.PracticeBase
                    : string.Empty,
                SupervisorId = student.QualificationWork != null
                    ? student.QualificationWork.TeacherId
                    : null,
                SupervisorFullName = student.QualificationWork != null && student.QualificationWork.Teacher != null
                    ? student.QualificationWork.Teacher.FullName
                    : null,
                SupervisorShortName = student.QualificationWork != null && student.QualificationWork.Teacher != null
                    ? student.QualificationWork.Teacher.ShortName
                    : null,
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

    internal static IReadOnlyCollection<StatisticSectionDto> BuildSections(
        IReadOnlyCollection<StudentProjection> students,
        int totalStudents)
    {
        return
        [
            BuildGradesSection(students, totalStudents),
            BuildWorkCharacterSection(students, totalStudents),
            BuildComplexDesignSection(students, totalStudents),
            BuildAdditionalSection(students, totalStudents),
            BuildPerformanceSection(students, totalStudents)
        ];
    }

    internal static double CalculatePercentage(int count, int totalStudents)
    {
        if (totalStudents == 0)
        {
            return 0;
        }

        return Math.Round((double)count * 100 / totalStudents, 1, MidpointRounding.AwayFromZero);
    }

    internal static double RoundAverage(double value)
    {
        return Math.Round(value, 1, MidpointRounding.AwayFromZero);
    }

    private static StatisticSectionDto BuildGradesSection(
        IReadOnlyCollection<StudentProjection> students,
        int totalStudents)
    {
        return new StatisticSectionDto(
            "gradesAndRecommendations",
            [
                CreateItem("excellent", Count(students, s => s.NationalGrade == NationalGrade.Excellent), totalStudents),
                CreateItem("good", Count(students, s => s.NationalGrade == NationalGrade.Good), totalStudents),
                CreateItem("satisfactory", Count(students, s => s.NationalGrade == NationalGrade.Satisfactory), totalStudents),
                CreateItem("diplomaWithHonors", Count(students, s => s.HasDiplomaWithHonors), totalStudents),
                CreateItem("recommendedForMaster", Count(students, s => s.IsRecommendedForMaster), totalStudents)
            ]);
    }

    private static StatisticSectionDto BuildWorkCharacterSection(
        IReadOnlyCollection<StudentProjection> students,
        int totalStudents)
    {
        return new StatisticSectionDto(
            "workCharacter",
            [
                CreateItem("researchBased", Count(students, s => s.IsResearchBased), totalStudents),
                CreateItem("realProjects", Count(students, s => s.HasRealProjects), totalStudents),
                CreateItem("ecoFriendly", Count(students, s => s.IsEcoFriendly), totalStudents),
                CreateItem("enterpriseOrdered", Count(students, s => s.IsEnterpriseOrdered), totalStudents)
            ]);
    }

    private static StatisticSectionDto BuildComplexDesignSection(
        IReadOnlyCollection<StudentProjection> students,
        int totalStudents)
    {
        return new StatisticSectionDto(
            "complexDiplomaDesign",
            [
                CreateItem("interuniversity", Count(students, s => s.IsComplexInteruniversity), totalStudents),
                CreateItem("interdepartmental", Count(students, s => s.IsComplexInterdepartmental), totalStudents),
                CreateItem("departmental", Count(students, s => s.IsComplexDepartmental), totalStudents),
                CreateItem("complexProjectParticipant", Count(students, s => s.IsComplexProjectParticipant), totalStudents)
            ]);
    }

    private static StatisticSectionDto BuildAdditionalSection(
        IReadOnlyCollection<StudentProjection> students,
        int totalStudents)
    {
        return new StatisticSectionDto(
            "additional",
            [
                CreateItem("recommendedForImplementation", Count(students, s => s.IsRecommendedForImplementation), totalStudents),
                CreateItem("defendedAtEnterprise", Count(students, s => s.IsDefendedAtEnterprise), totalStudents)
            ]);
    }

    private static StatisticSectionDto BuildPerformanceSection(
        IReadOnlyCollection<StudentProjection> students,
        int totalStudents)
    {
        var excellentCount = Count(students, s => s.NationalGrade == NationalGrade.Excellent);
        var goodCount = Count(students, s => s.NationalGrade == NationalGrade.Good);
        var satisfactoryCount = Count(students, s => s.NationalGrade == NationalGrade.Satisfactory);

        return new StatisticSectionDto(
            "performanceIndicators",
            [
                CreateItem("educationQuality", excellentCount + goodCount, totalStudents),
                CreateItem("overallSuccess", excellentCount + goodCount + satisfactoryCount, totalStudents)
            ]);
    }

    private static StatisticItemDto CreateItem(
        string key,
        int count,
        int totalStudents)
    {
        return new StatisticItemDto(key, count, CalculatePercentage(count, totalStudents));
    }

    private static int Count(
        IEnumerable<StudentProjection> students,
        Func<StudentProjection, bool> predicate)
    {
        return students.Count(predicate);
    }
}
