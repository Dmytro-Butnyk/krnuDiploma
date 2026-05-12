using Core.Domain.ResultPattern;

namespace DocumentGenerationSubsystem.Api.ErrorsAndLogs;

internal static class DynamicDocumentEngineErrors
{
    public static readonly ErrorDetails InvalidConfiguration = ErrorDetails.Validation("DocGen.InvalidConfig", "Failed to parse template configuration.");
    public static readonly ErrorDetails DynamicLinqError = ErrorDetails.Validation("DocGen.DynamicLinqError", "Invalid filter syntax.");
    public static readonly ErrorDetails NestedListNotSupported = ErrorDetails.Validation("DocGen.NestedListNotSupported", "MiniWord does not support nested lists > 2 levels. Flatten your data.");
    
    public static readonly ErrorDetails UnauthorizedEntity = ErrorDetails.Forbidden("DocGen.UnauthorizedEntity", "Security violation or unknown entity requested.");
    
    public static readonly ErrorDetails DatabaseError = ErrorDetails.Failure("DocGen.DatabaseError", "An error occurred while fetching data.");
    public static readonly ErrorDetails MiniWordGenerationFailed = ErrorDetails.Failure("DocGen.MiniWordError", "Failed to generate document via MiniWord. Check template tags.");
    
    public static ErrorDetails MissingParameter(string parameterName, string sourceKey) =>
        ErrorDetails.Validation(
            "DocGen.MissingParameter",
            $"Missing required parameter: '{parameterName}' for '{sourceKey}'.");
}

