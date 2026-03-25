using Core.Domain.ResultPattern;

namespace DocumentGenerationSubsystem.Application.Interfaces;

public interface IDocumentGeneratorEngine
{
    Task<Result<Stream>> GenerateAsync(
        string configurationJson, 
        byte[] wordTemplate, 
        IReadOnlyDictionary<string, string> parameters, 
        CancellationToken cancellationToken);
}
