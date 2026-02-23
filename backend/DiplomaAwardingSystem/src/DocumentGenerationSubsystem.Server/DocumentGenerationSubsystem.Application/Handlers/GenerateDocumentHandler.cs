using Core.Domain.ResultPattern;
using DocumentGenerationSubsystem.Application.Dto;
using DocumentGenerationSubsystem.Application.Interfaces;
using DocumentGenerationSubsystem.Domain.DependencyInjectionInterfaces;
using DocumentGenerationSubsystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocumentGenerationSubsystem.Application.Handlers;

public sealed class GenerateDocumentHandler(
    IDbDocGenContext context,
    IDocumentGeneratorEngine documentEngine) : IScopedService
{
    public async Task<Result<(Stream Stream, string FileName)>> HandleAsync(
        GenerateDocumentRequest request, 
        CancellationToken cancellationToken)
    {
        DocumentTemplate? template = await context.DocumentTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TemplateId, cancellationToken);

        if (template is null)
        {
            return new ErrorDetails("NotFound", $"Template with ID {request.TemplateId} not found.");
        }

        Stream documentStream = await documentEngine.GenerateAsync(
            template.ConfigurationJson,
            (byte[])template.WordTemplate,
            request.Parameters,
            cancellationToken);

        if (documentStream.CanSeek)
        {
            documentStream.Position = 0;
        }

        string fileName = $"{template.Name}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.docx";

        return (documentStream, fileName);
    }
}
