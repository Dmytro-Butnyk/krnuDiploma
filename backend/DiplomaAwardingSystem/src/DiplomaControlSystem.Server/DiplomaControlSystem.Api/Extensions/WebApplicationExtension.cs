using DiplomaControlSystem.Api.Features.CommissionHeads;
using DiplomaControlSystem.Api.Features.DiplomaExaminationCommissions;
using DiplomaControlSystem.Api.Features.Groups;
using DiplomaControlSystem.Api.Features.Students;

namespace DiplomaControlSystem.Api.Extensions;

public static class WebApplicationExtension
{
    public static void MapAllEndpoints(this WebApplication app)
    {
        var apiGroup = app.MapGroup("/api");

        CreateCommissionHead.Endpoint.MapEndpoint(apiGroup);
        DeleteCommissionHead.Endpoint.MapEndpoint(apiGroup);
        GetCommissionHeads.Endpoint.MapEndpoint(apiGroup);
        UpdateCommissionHead.Endpoint.MapEndpoint(apiGroup);

        CreateDiplomaExaminationCommission.Endpoint.MapEndpoint(apiGroup);
        DeleteDiplomaExaminationCommission.Endpoint.MapEndpoint(apiGroup);
        GetDiplomaExaminationCommission.Endpoint.MapEndpoint(apiGroup);
        GetDiplomaExaminationCommissionOptions.Endpoint.MapEndpoint(apiGroup);
        UpdateDiplomaExaminationCommission.Endpoint.MapEndpoint(apiGroup);

        CreateGroup.Endpoint.MapEndpoint(apiGroup);
        DeleteGroup.Endpoint.MapEndpoint(apiGroup);
        GetAcademicYearsOverview.Endpoint.MapEndpoint(apiGroup);
        GetGroupStatistics.Endpoint.MapEndpoint(apiGroup);
        GetGroupStudents.Endpoint.MapEndpoint(apiGroup);
        UpdateGroup.Endpoint.MapEndpoint(apiGroup);

        AddStudent.Endpoint.MapEndpoint(apiGroup);
        DeleteStudent.Endpoint.MapEndpoint(apiGroup);
        GetQualificationWorkOptions.Endpoint.MapEndpoint(apiGroup);
        GetStudentDetails.Endpoint.MapEndpoint(apiGroup);
        UpdateDefenceResults.Endpoint.MapEndpoint(apiGroup);
        UpdateElectronicChecklist.Endpoint.MapEndpoint(apiGroup);
        UpdatePhysicalChecklist.Endpoint.MapEndpoint(apiGroup);
        UpdateQualificationWorkCharacteristics.Endpoint.MapEndpoint(apiGroup);
        UpdateStudentDefence.Endpoint.MapEndpoint(apiGroup);
        UpdateStudentName.Endpoint.MapEndpoint(apiGroup);
        UpdateStudentQualificationWork.Endpoint.MapEndpoint(apiGroup);
    }
}
