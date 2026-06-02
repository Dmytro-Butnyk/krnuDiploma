using DocumentGenerationSubsystem.Api.Features.Constructor;
using DocumentGenerationSubsystem.Api.Features.Documents;

namespace DocumentGenerationSubsystem.Api.Extensions;

public static class WebApplicationExtension
{
    public static void MapAllEndpoints(this WebApplication app)
    {
        var apiGroup = app.MapGroup("/api");
        
        GenerateDocument.Endpoint.MapEndpoint(apiGroup);
        GetGenerationInputOptions.Endpoint.MapEndpoint(apiGroup);
        GetTemplateGenerationForm.Endpoint.MapEndpoint(apiGroup);
        GetConstructorScenarios.Endpoint.MapEndpoint(apiGroup);
        GetTemplateSchema.Endpoint.MapEndpoint(apiGroup);
        ScanTemplateForTags.Endpoint.MapEndpoint(apiGroup);
        
        UploadTemplate.Endpoint.MapEndpoint(apiGroup);
        UpdateTemplate.Endpoint.MapEndpoints(apiGroup);
        DeleteTemplate.Endpoint.MapEndpoints(apiGroup);
        
        GetTemplateDetails.Endpoint.MapEndpoints(apiGroup);
        GetTemplatesList.Endpoint.MapEndpoints(apiGroup);
        DownloadTemplate.Endpoint.MapEndpoints(apiGroup);
    }
}
