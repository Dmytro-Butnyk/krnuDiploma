using Core.Domain.Entities;

#pragma warning disable CA1819

namespace DocumentGenerationSubsystem.Api.Entities;

public sealed class DocumentTemplate : BaseEntity 
{
    public string Name { get; internal set; } = string.Empty;

    public byte[] WordTemplate { get; internal set; } = [];
    
    public string ConfigurationJson { get; internal set; } = string.Empty;
    
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
