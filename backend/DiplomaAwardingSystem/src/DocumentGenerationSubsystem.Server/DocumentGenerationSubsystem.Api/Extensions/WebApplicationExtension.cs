namespace DocumentGenerationSubsystem.Api.Extensions;

public static class WebApplicationExtension
{
    public static void MapAllEndpoints(this WebApplication app)
    {
    }

    public static async Task EnsureDatabaseExistAndMigrationsApplied(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
    }

    // public static async Task EnsureRolesValid(this WebApplication app)
    // {
        // await using var scope = app.Services.CreateAsyncScope();
        // var services = scope.ServiceProvider;
        //
        // var context = services.GetRequiredService<IdentityContext>();
        // var roleManager = services.GetRequiredService<RoleManager<Role>>();
        //
        // await context.EnsureAllRolesExist(roleManager);
    // }
}
