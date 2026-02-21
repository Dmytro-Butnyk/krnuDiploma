namespace DocumentGenerationSubsystem.Api.Extensions;

public static class WebApplicationExtension
{
    public static void MapAllEndpoints(this WebApplication app)
    {
    }

    // public static async Task EnsureDatabaseExistAndMigrationsApplied(this WebApplication app)
    // {
    //     await using var scope = app.Services.CreateAsyncScope();
    //     var services = scope.ServiceProvider;
    // }
}
