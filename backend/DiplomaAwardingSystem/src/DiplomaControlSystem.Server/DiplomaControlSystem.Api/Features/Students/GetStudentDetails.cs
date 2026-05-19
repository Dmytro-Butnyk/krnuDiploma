using Core.Api.Extensions;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.Enums;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using DiplomaControlSystem.Api.Infrastructure.Access;
using DiplomaControlSystem.Api.Infrastructure.Students;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DiplomaControlSystem.Api.Features.Students;

public static class GetStudentDetails
{
    public sealed record StudentNameDto(string LastName, string FirstName, string MiddleName);

    public sealed record QualificationWorkDto(
        string Topic,
        int? SupervisorId,
        string? SupervisorName,
        string PracticeBase,
        int? ReviewerId,
        string? ReviewerName);

    public sealed record PhysicalChecklistDto(
        bool HasStudentCard,
        bool HasGradeBook,
        bool HasCircular,
        bool HasSignedReview,
        bool HasCopyOfBankReceipt,
        bool HasExplanatoryNote);

    public sealed record ElectronicChecklistDto(
        bool HasRegulatoryControl,
        bool HasExplanatoryNoteDoc,
        bool HasExplanatoryNotePdf,
        bool HasPlagiarismReportPdf,
        bool HasReviewDoc,
        bool HasPresentationPpt);

    public sealed record DefenceInfoDto(DateOnly? DefenceDate);

    public sealed record DefenceResultsDto(
        float PlagiarismPercent,
        float UniquePercent,
        int SupervisorScore,
        int ReviewerScore,
        int CommissionScore,
        string EctsGrade,
        string NationalGrade,
        bool HasDiplomaWithHonors);

    public sealed record CharacteristicsDto(
        bool IsResearchBased,
        bool HasRealProjects,
        bool IsEcoFriendly,
        bool IsEnterpriseOrdered,
        bool IsComplexInteruniversity,
        bool IsComplexInterdepartmental,
        bool IsComplexDepartmental,
        bool IsComplexProjectParticipant,
        bool IsRecommendedForMaster,
        bool IsRecommendedForImplementation,
        bool IsDefendedAtEnterprise);

    public sealed record Response(
        int Id,
        int GroupId,
        string FullName,
        StudentNameDto Name,
        QualificationWorkDto? QualificationWork,
        PhysicalChecklistDto? PhysicalChecklist,
        ElectronicChecklistDto? ElectronicChecklist,
        DefenceInfoDto? DefenceInfo,
        DefenceResultsDto? DefenceResults,
        CharacteristicsDto? Characteristics);

    internal static class Endpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/students/{studentId:int}/details", Handle)
                .WithSummary("Gets full student diploma process details")
                .Produces<Response>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithTags("Students");
        }

        private static async Task<Results<Ok<Response>, ProblemHttpResult>> Handle(
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
        public async Task<Result<Response>> HandleAsync(
            int studentId,
            string secretaryEmail,
            CancellationToken ct)
        {
            var accessResult = await studentAccessService.GetForSecretaryAsync(studentId, secretaryEmail, ct);
            if (accessResult.IsFailure)
            {
                return accessResult.ErrorDetails;
            }

            var student = await context.Students
                .AsNoTracking()
                .Where(s => s.Id == studentId)
                .Select(s => new StudentDetailsProjection
                {
                    Id = s.Id,
                    GroupId = s.GroupId,
                    FullName = s.FullName,
                    QualificationWorkId = s.QualificationWork != null
                        ? (int?)s.QualificationWork.Id
                        : null,
                    Topic = s.QualificationWork != null
                        ? s.QualificationWork.Topic
                        : null,
                    PracticeBase = s.QualificationWork != null
                        ? s.QualificationWork.PracticeBase
                        : null,
                    SupervisorId = s.QualificationWork != null
                        ? s.QualificationWork.TeacherId
                        : null,
                    SupervisorName = s.QualificationWork != null && s.QualificationWork.Teacher != null
                        ? s.QualificationWork.Teacher.ShortName
                        : null,
                    ReviewerId = s.QualificationWork != null
                        ? s.QualificationWork.ReviewerId
                        : null,
                    ReviewerName = s.QualificationWork != null && s.QualificationWork.Reviewer != null
                        ? s.QualificationWork.Reviewer.ShortName
                        : null,
                    PlagiarismPercent = s.QualificationWork != null
                        ? (float?)s.QualificationWork.PlagiarismPercent
                        : null,
                    UniquePercent = s.QualificationWork != null
                        ? (float?)s.QualificationWork.UniquePercent
                        : null,
                    SupervisorScore = s.QualificationWork != null
                        ? (int?)s.QualificationWork.SupervisorScore
                        : null,
                    ReviewerScore = s.QualificationWork != null
                        ? (int?)s.QualificationWork.ReviewerScore
                        : null,
                    CommissionScore = s.QualificationWork != null
                        ? (int?)s.QualificationWork.CommissionScore
                        : null,
                    EctsGrade = s.QualificationWork != null
                        ? (EctsGrade?)s.QualificationWork.EctsGrade
                        : null,
                    NationalGrade = s.QualificationWork != null
                        ? (NationalGrade?)s.QualificationWork.NationalGrade
                        : null,
                    HasDiplomaWithHonors = s.QualificationWork != null
                        ? (bool?)s.QualificationWork.HasDiplomaWithHonors
                        : null,
                    DefenceDate = s.QualificationWork != null
                        ? s.QualificationWork.DefenceDate
                        : null,
                    HasDefence = s.QualificationWork != null,
                    HasPhysicalChecklist = s.PhysicalComponentsChecklist != null,
                    PhysicalHasStudentCard = s.PhysicalComponentsChecklist != null && s.PhysicalComponentsChecklist.HasStudentCard,
                    PhysicalHasGradeBook = s.PhysicalComponentsChecklist != null && s.PhysicalComponentsChecklist.HasGradeBook,
                    PhysicalHasCircular = s.PhysicalComponentsChecklist != null && s.PhysicalComponentsChecklist.HasCircular,
                    PhysicalHasSignedReview = s.PhysicalComponentsChecklist != null && s.PhysicalComponentsChecklist.HasSignedReview,
                    PhysicalHasCopyOfBankReceipt = s.PhysicalComponentsChecklist != null && s.PhysicalComponentsChecklist.HasCopyOfBankReceipt,
                    PhysicalHasExplanatoryNote = s.PhysicalComponentsChecklist != null && s.PhysicalComponentsChecklist.HasExplanatoryNote,
                    HasElectronicChecklist = s.ElectronicComponentsChecklist != null,
                    ElectronicHasRegulatoryControl = s.ElectronicComponentsChecklist != null && s.ElectronicComponentsChecklist.HasRegulatoryControl,
                    ElectronicHasExplanatoryNoteDoc = s.ElectronicComponentsChecklist != null && s.ElectronicComponentsChecklist.HasExplanatoryNoteDoc,
                    ElectronicHasExplanatoryNotePdf = s.ElectronicComponentsChecklist != null && s.ElectronicComponentsChecklist.HasExplanatoryNotePdf,
                    ElectronicHasPlagiarismReportPdf = s.ElectronicComponentsChecklist != null && s.ElectronicComponentsChecklist.HasPlagiarismReportPdf,
                    ElectronicHasReviewDoc = s.ElectronicComponentsChecklist != null && s.ElectronicComponentsChecklist.HasReviewDoc,
                    ElectronicHasPresentationPpt = s.ElectronicComponentsChecklist != null && s.ElectronicComponentsChecklist.HasPresentationPpt,
                    HasCharacteristics = s.QualificationWork != null && s.QualificationWork.QualificationWorkCharacteristics != null,
                    IsResearchBased = s.QualificationWork != null && s.QualificationWork.QualificationWorkCharacteristics != null && s.QualificationWork.QualificationWorkCharacteristics.IsResearchBased,
                    HasRealProjects = s.QualificationWork != null && s.QualificationWork.QualificationWorkCharacteristics != null && s.QualificationWork.QualificationWorkCharacteristics.HasRealProjects,
                    IsEcoFriendly = s.QualificationWork != null && s.QualificationWork.QualificationWorkCharacteristics != null && s.QualificationWork.QualificationWorkCharacteristics.IsEcoFriendly,
                    IsEnterpriseOrdered = s.QualificationWork != null && s.QualificationWork.QualificationWorkCharacteristics != null && s.QualificationWork.QualificationWorkCharacteristics.IsEnterpriseOrdered,
                    IsComplexInteruniversity = s.QualificationWork != null && s.QualificationWork.QualificationWorkCharacteristics != null && s.QualificationWork.QualificationWorkCharacteristics.IsComplexInteruniversity,
                    IsComplexInterdepartmental = s.QualificationWork != null && s.QualificationWork.QualificationWorkCharacteristics != null && s.QualificationWork.QualificationWorkCharacteristics.IsComplexInterdepartmental,
                    IsComplexDepartmental = s.QualificationWork != null && s.QualificationWork.QualificationWorkCharacteristics != null && s.QualificationWork.QualificationWorkCharacteristics.IsComplexDepartmental,
                    IsComplexProjectParticipant = s.QualificationWork != null && s.QualificationWork.QualificationWorkCharacteristics != null && s.QualificationWork.QualificationWorkCharacteristics.IsComplexProjectParticipant,
                    IsRecommendedForMaster = s.QualificationWork != null && s.QualificationWork.QualificationWorkCharacteristics != null && s.QualificationWork.QualificationWorkCharacteristics.IsRecommendedForMaster,
                    IsRecommendedForImplementation = s.QualificationWork != null && s.QualificationWork.QualificationWorkCharacteristics != null && s.QualificationWork.QualificationWorkCharacteristics.IsRecommendedForImplementation,
                    IsDefendedAtEnterprise = s.QualificationWork != null && s.QualificationWork.QualificationWorkCharacteristics != null && s.QualificationWork.QualificationWorkCharacteristics.IsDefendedAtEnterprise
                })
                .FirstOrDefaultAsync(ct);

            if (student is null)
            {
                return ErrorDetails.NotFound(
                    "Student.NotFound",
                    "Student was not found.");
            }

            return MapResponse(student);
        }

        private static Response MapResponse(StudentDetailsProjection student)
        {
            var name = StudentNameParser.Parse(student.FullName);

            return new Response(
                student.Id,
                student.GroupId,
                student.FullName,
                new StudentNameDto(name.LastName, name.FirstName, name.MiddleName),
                MapQualificationWork(student),
                MapPhysicalChecklist(student),
                MapElectronicChecklist(student),
                MapDefenceInfo(student),
                MapDefenceResults(student),
                MapCharacteristics(student));
        }

        private static QualificationWorkDto? MapQualificationWork(StudentDetailsProjection student)
        {
            if (student.QualificationWorkId is null)
            {
                return null;
            }

            return new QualificationWorkDto(
                student.Topic ?? string.Empty,
                student.SupervisorId,
                student.SupervisorName,
                student.PracticeBase ?? string.Empty,
                student.ReviewerId,
                student.ReviewerName);
        }

        private static PhysicalChecklistDto? MapPhysicalChecklist(StudentDetailsProjection student)
        {
            if (!student.HasPhysicalChecklist)
            {
                return null;
            }

            return new PhysicalChecklistDto(
                student.PhysicalHasStudentCard,
                student.PhysicalHasGradeBook,
                student.PhysicalHasCircular,
                student.PhysicalHasSignedReview,
                student.PhysicalHasCopyOfBankReceipt,
                student.PhysicalHasExplanatoryNote);
        }

        private static ElectronicChecklistDto? MapElectronicChecklist(StudentDetailsProjection student)
        {
            if (!student.HasElectronicChecklist)
            {
                return null;
            }

            return new ElectronicChecklistDto(
                student.ElectronicHasRegulatoryControl,
                student.ElectronicHasExplanatoryNoteDoc,
                student.ElectronicHasExplanatoryNotePdf,
                student.ElectronicHasPlagiarismReportPdf,
                student.ElectronicHasReviewDoc,
                student.ElectronicHasPresentationPpt);
        }

        private static DefenceInfoDto? MapDefenceInfo(StudentDetailsProjection student)
        {
            return student.HasDefence
                ? new DefenceInfoDto(student.DefenceDate)
                : null;
        }

        private static DefenceResultsDto? MapDefenceResults(StudentDetailsProjection student)
        {
            if (student.QualificationWorkId is null)
            {
                return null;
            }

            return new DefenceResultsDto(
                student.PlagiarismPercent ?? 0,
                student.UniquePercent ?? 0,
                student.SupervisorScore ?? 0,
                student.ReviewerScore ?? 0,
                student.CommissionScore ?? 0,
                (student.EctsGrade ?? EctsGrade.None).ToString(),
                (student.NationalGrade ?? NationalGrade.None).ToString(),
                student.HasDiplomaWithHonors ?? false);
        }

        private static CharacteristicsDto? MapCharacteristics(StudentDetailsProjection student)
        {
            if (!student.HasCharacteristics)
            {
                return null;
            }

            return new CharacteristicsDto(
                student.IsResearchBased,
                student.HasRealProjects,
                student.IsEcoFriendly,
                student.IsEnterpriseOrdered,
                student.IsComplexInteruniversity,
                student.IsComplexInterdepartmental,
                student.IsComplexDepartmental,
                student.IsComplexProjectParticipant,
                student.IsRecommendedForMaster,
                student.IsRecommendedForImplementation,
                student.IsDefendedAtEnterprise);
        }
    }

    private sealed class StudentDetailsProjection
    {
        public int Id { get; init; }
        public int GroupId { get; init; }
        public string FullName { get; init; } = string.Empty;
        public int? QualificationWorkId { get; init; }
        public string? Topic { get; init; }
        public string? PracticeBase { get; init; }
        public int? SupervisorId { get; init; }
        public string? SupervisorName { get; init; }
        public int? ReviewerId { get; init; }
        public string? ReviewerName { get; init; }
        public float? PlagiarismPercent { get; init; }
        public float? UniquePercent { get; init; }
        public int? SupervisorScore { get; init; }
        public int? ReviewerScore { get; init; }
        public int? CommissionScore { get; init; }
        public EctsGrade? EctsGrade { get; init; }
        public NationalGrade? NationalGrade { get; init; }
        public bool? HasDiplomaWithHonors { get; init; }
        public DateOnly? DefenceDate { get; init; }
        public bool HasDefence { get; init; }
        public bool HasPhysicalChecklist { get; init; }
        public bool PhysicalHasStudentCard { get; init; }
        public bool PhysicalHasGradeBook { get; init; }
        public bool PhysicalHasCircular { get; init; }
        public bool PhysicalHasSignedReview { get; init; }
        public bool PhysicalHasCopyOfBankReceipt { get; init; }
        public bool PhysicalHasExplanatoryNote { get; init; }
        public bool HasElectronicChecklist { get; init; }
        public bool ElectronicHasRegulatoryControl { get; init; }
        public bool ElectronicHasExplanatoryNoteDoc { get; init; }
        public bool ElectronicHasExplanatoryNotePdf { get; init; }
        public bool ElectronicHasPlagiarismReportPdf { get; init; }
        public bool ElectronicHasReviewDoc { get; init; }
        public bool ElectronicHasPresentationPpt { get; init; }
        public bool HasCharacteristics { get; init; }
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
