using DiplomaControlSystem.Api.Features.Groups;

namespace DiplomaControlSystem.Api.Extensions;

public static class WebApplicationExtension
{
    public static void MapAllEndpoints(this WebApplication app)
    {
        var apiGroup = app.MapGroup("/api");

        GetAcademicYearsOverview.Endpoint.MapEndpoint(apiGroup);
    }
}
