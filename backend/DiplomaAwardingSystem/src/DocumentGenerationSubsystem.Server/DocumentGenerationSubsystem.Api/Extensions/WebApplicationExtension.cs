using DocumentGenerationSubsystem.Api.Features.Constructor;
using DocumentGenerationSubsystem.Api.Features.Documents;

namespace DocumentGenerationSubsystem.Api.Extensions;

public static class WebApplicationExtension
{
    public static void MapAllEndpoints(this WebApplication app)
    {
        var apiGroup = app.MapGroup("/api");
        
        GenerateDocument.Endpoint.MapEndpoint(apiGroup);
        UploadTemplate.Endpoint.MapEndpoint(apiGroup);
        GetTemplateSchema.Endpoint.MapEndpoint(apiGroup);
    }

    // public static async Task EnsureDatabaseExistAndMigrationsApplied(this WebApplication app)
    // {
    //     await using var scope = app.Services.CreateAsyncScope();
    //     var services = scope.ServiceProvider;
    // }
}
