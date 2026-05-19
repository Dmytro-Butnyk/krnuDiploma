using DiplomaControlSystem.Api.Features.Groups;
using DiplomaControlSystem.Api.Features.Students;

namespace DiplomaControlSystem.Api.Extensions;

public static class WebApplicationExtension
{
    public static void MapAllEndpoints(this WebApplication app)
    {
        var apiGroup = app.MapGroup("/api");

        CreateGroup.Endpoint.MapEndpoint(apiGroup);
        DeleteGroup.Endpoint.MapEndpoint(apiGroup);
        GetAcademicYearsOverview.Endpoint.MapEndpoint(apiGroup);
        UpdateGroup.Endpoint.MapEndpoint(apiGroup);

        AddStudent.Endpoint.MapEndpoint(apiGroup);
        DeleteStudent.Endpoint.MapEndpoint(apiGroup);
    }
}
