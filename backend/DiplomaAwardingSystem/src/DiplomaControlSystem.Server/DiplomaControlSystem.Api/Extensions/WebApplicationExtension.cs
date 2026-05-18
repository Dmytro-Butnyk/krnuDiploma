namespace DiplomaControlSystem.Api.Extensions;

public static class WebApplicationExtension
{
    public static void MapAllEndpoints(this WebApplication app)
    {
        app.MapGroup("/api");
    }
}
