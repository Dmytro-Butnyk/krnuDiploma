using Core.Api.Extensions;
using Core.Domain.DependencyInjectionInterfaces;
using Core.Domain.ResultPattern;
using Core.Infrastructure;
using DiplomaControlSystem.Api.Infrastructure.Access;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DiplomaControlSystem.Api.Features.Groups;

public static class GetGroupStudents
{
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

    public sealed record GetGroupStudentsResponse(
        int Id,
        string FullName,
        string? SupervisorName,
        PhysicalChecklistDto? PhysicalChecklist,
        ElectronicChecklistDto? ElectronicChecklist);

    internal static class Endpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/groups/{groupId:int}/students", Handle)
                .WithSummary("Gets students with checklist data for a group")
                .Produces<IReadOnlyCollection<GetGroupStudentsResponse>>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithTags("Groups");
        }

        private static async Task<Results<Ok<IReadOnlyCollection<GetGroupStudentsResponse>>, ProblemHttpResult>> Handle(
            [FromRoute] int groupId,
            [FromQuery] string secretaryEmail,
            [FromServices] Handler handler,
            CancellationToken ct)
        {
            var result = await handler.HandleAsync(groupId, secretaryEmail, ct);

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
        public async Task<Result<IReadOnlyCollection<GetGroupStudentsResponse>>> HandleAsync(
            int groupId,
            string secretaryEmail,
            CancellationToken ct)
        {
            var secretaryResult = await secretaryAccessService.GetActiveSecretaryAsync(secretaryEmail, ct);
            if (secretaryResult.IsFailure)
            {
                return secretaryResult.ErrorDetails;
            }

            var secretary = secretaryResult.Value!;
            var groupSpecialtyId = await context.Groups
                .AsNoTracking()
                .Where(g => g.Id == groupId)
                .Select(g => (int?)g.SpecialtyId)
                .FirstOrDefaultAsync(ct);

            if (groupSpecialtyId is null)
            {
                return ErrorDetails.NotFound(
                    "Group.NotFound",
                    "Group was not found.");
            }

            if (groupSpecialtyId != secretary.SpecialtyId)
            {
                return ErrorDetails.Forbidden(
                    "Group.Forbidden",
                    "Group does not belong to secretary specialty.");
            }

            var students = await context.Students
                .AsNoTracking()
                .Where(s => s.GroupId == groupId)
                .OrderBy(s => s.FullName)
                .Select(s => new
                {
                    s.Id,
                    s.FullName,
                    SupervisorName = s.QualificationWork != null && s.QualificationWork.Teacher != null
                        ? s.QualificationWork.Teacher.ShortName
                        : null,
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
                    ElectronicHasPresentationPpt = s.ElectronicComponentsChecklist != null && s.ElectronicComponentsChecklist.HasPresentationPpt
                })
                .ToListAsync(ct);

            var response = new List<GetGroupStudentsResponse>(students.Count);

            foreach (var student in students)
            {
                PhysicalChecklistDto? physicalChecklist = null;
                if (student.HasPhysicalChecklist)
                {
                    physicalChecklist = new PhysicalChecklistDto(
                        student.PhysicalHasStudentCard,
                        student.PhysicalHasGradeBook,
                        student.PhysicalHasCircular,
                        student.PhysicalHasSignedReview,
                        student.PhysicalHasCopyOfBankReceipt,
                        student.PhysicalHasExplanatoryNote);
                }

                ElectronicChecklistDto? electronicChecklist = null;
                if (student.HasElectronicChecklist)
                {
                    electronicChecklist = new ElectronicChecklistDto(
                        student.ElectronicHasRegulatoryControl,
                        student.ElectronicHasExplanatoryNoteDoc,
                        student.ElectronicHasExplanatoryNotePdf,
                        student.ElectronicHasPlagiarismReportPdf,
                        student.ElectronicHasReviewDoc,
                        student.ElectronicHasPresentationPpt);
                }

                response.Add(new GetGroupStudentsResponse(
                    student.Id,
                    student.FullName,
                    student.SupervisorName,
                    physicalChecklist,
                    electronicChecklist));
            }

            return response;
        }
    }
}
