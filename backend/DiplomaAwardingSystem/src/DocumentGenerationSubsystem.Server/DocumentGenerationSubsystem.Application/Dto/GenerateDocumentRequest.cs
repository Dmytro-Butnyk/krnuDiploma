namespace DocumentGenerationSubsystem.Application.Dto;

public sealed record GenerateDocumentRequest(
    int TemplateId,
    Dictionary<string, string> Parameters
);
