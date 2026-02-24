using Microsoft.AspNetCore.Http;

namespace DocumentGenerationSubsystem.Application.Dto;

public record UploadTemplateRequest(
    string Name,
    string ConfigurationJson,
    IFormFile File
);
