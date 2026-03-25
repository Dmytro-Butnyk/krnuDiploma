using Core.Domain.ResultPattern;

namespace DocumentGenerationSubsystem.Domain.Entities.ErrorDetailsDescriptions;

public static class DocumentErrors
{
    public static readonly ErrorDetails InvalidConfiguration = new("DocGen.InvalidConfig", "Failed to parse template configuration.");
    public static readonly ErrorDetails UnauthorizedEntity = new("DocGen.UnauthorizedEntity", "Security violation or unknown entity requested.");
    public static readonly ErrorDetails DatabaseError = new("DocGen.DatabaseError", "An error occurred while fetching data.");
    public static readonly ErrorDetails DynamicLinqError = new("DocGen.DynamicLinqError", "Invalid filter syntax.");
    public static readonly ErrorDetails MiniWordGenerationFailed = new("DocGen.MiniWordError", "Failed to generate document via MiniWord. Check template tags.");
    public static readonly ErrorDetails NestedListNotSupported = new("DocGen.NestedListNotSupported", "MiniWord does not support nested lists > 2 levels. Flatten your data.");
}
