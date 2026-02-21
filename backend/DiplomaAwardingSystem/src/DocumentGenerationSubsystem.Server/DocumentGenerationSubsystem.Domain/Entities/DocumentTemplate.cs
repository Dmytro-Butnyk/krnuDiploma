using Core.Domain.Entities;

namespace DocumentGenerationSubsystem.Domain.Entities;

public sealed class DocumentTemplate : BaseEntity 
{
    public string Name { get; init; } = string.Empty;

    private readonly byte[] _wordTemplate = [];
    public IReadOnlyList<byte> WordTemplate => _wordTemplate;
    
    public string ConfigurationJson { get; init; } = string.Empty;
    
    private DocumentTemplate()
    {
    }

    public DocumentTemplate(string name, byte[] wordTemplate, string configurationJson)
    {
        Name = name;
        _wordTemplate = wordTemplate;
        ConfigurationJson = configurationJson;
    }
}
