using System.Diagnostics.CodeAnalysis;
using Core.Domain.Entities;

#pragma warning disable CA1819

namespace DocumentGenerationSubsystem.Domain.Entities;

public sealed class DocumentTemplate : BaseEntity 
{
    public string Name { get; init; } = string.Empty;

    public byte[] WordTemplate { get; init; } = [];
    
    public string ConfigurationJson { get; init; } = string.Empty;
    
    private DocumentTemplate()
    {   
    }

    public DocumentTemplate(string name, byte[] wordTemplate, string configurationJson)
    {
        Name = name;
        WordTemplate = wordTemplate;
        ConfigurationJson = configurationJson;
    }
}
