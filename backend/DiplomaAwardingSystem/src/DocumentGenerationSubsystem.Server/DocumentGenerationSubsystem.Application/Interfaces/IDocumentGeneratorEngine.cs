namespace DocumentGenerationSubsystem.Application.Interfaces;

public interface IDocumentGeneratorEngine
{
    Task<Stream> GenerateAsync(
        string configurationJson, 
        byte[] wordTemplate, 
        IReadOnlyDictionary<string, string> parameters, 
        CancellationToken cancellationToken);
}
