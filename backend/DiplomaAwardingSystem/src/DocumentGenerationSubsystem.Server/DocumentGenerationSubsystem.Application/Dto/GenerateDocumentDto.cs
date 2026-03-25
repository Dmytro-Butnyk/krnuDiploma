namespace DocumentGenerationSubsystem.Application.Dto;

public sealed record GenerateDocumentDto(
    int TemplateId,
    Dictionary<string, string> Parameters
);
