namespace DocumentGenerationSubsystem.Api.ErrorsAndLogs;

internal static partial class DynamicDocumentEngineLogger
{
    [LoggerMessage(EventId = 2001, Level = LogLevel.Error, Message = "Failed to deserialize template configuration.")]
    public static partial void LogDeserializationError(this ILogger logger, Exception exception);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Warning, Message = "Missing required parameter '{Parameter}' for data source '{SourceKey}'")]
    public static partial void LogMissingParameter(this ILogger logger, string parameter, string sourceKey);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Error, Message = "MiniWord engine crashed during document generation.")]
    public static partial void LogMiniWordCrash(this ILogger logger, Exception exception);

    [LoggerMessage(EventId = 2004, Level = LogLevel.Warning, Message = "Invalid dynamic LINQ syntax for entity {Entity}")]
    public static partial void LogDynamicLinqError(this ILogger logger, Exception exception, string entity);

    [LoggerMessage(EventId = 2005, Level = LogLevel.Error, Message = "Database error during data fetching for entity {Entity}")]
    public static partial void LogDatabaseError(this ILogger logger, Exception exception, string entity);
    
    [LoggerMessage(EventId = 2006, Level = LogLevel.Information, Message = "Started document generation for config length: {ConfigLength} bytes.")]
    public static partial void LogGenerationStarted(this ILogger logger, int configLength);

    [LoggerMessage(EventId = 2007, Level = LogLevel.Information, Message = "Successfully generated document. Data sources processed: {DataSourceCount}")]
    public static partial void LogGenerationCompleted(this ILogger logger, int dataSourceCount);

    [LoggerMessage(EventId = 2008, Level = LogLevel.Warning, Message = "Attempted to query unauthorized or unknown entity: '{Entity}'")]
    public static partial void LogUnauthorizedEntity(this ILogger logger, string entity);

    [LoggerMessage(EventId = 2009, Level = LogLevel.Warning, Message = "Mapping failed before Word generation. Reason: {ErrorCode} - {ErrorMessage}")]
    public static partial void LogMappingFailed(this ILogger logger, string errorCode, string errorMessage);
}
