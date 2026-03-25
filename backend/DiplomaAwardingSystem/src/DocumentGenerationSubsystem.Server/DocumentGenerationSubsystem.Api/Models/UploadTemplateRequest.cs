namespace DocumentGenerationSubsystem.Api.Models;

public record UploadTemplateRequest(
    string Name,
    string ConfigurationJson,
    IFormFile File
);
