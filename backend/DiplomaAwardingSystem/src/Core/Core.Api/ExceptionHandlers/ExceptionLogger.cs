using Microsoft.Extensions.Logging;

namespace Core.Api.ExceptionHandlers;

/// <summary>
/// Provides logging functionality for exception handling.
/// </summary>
internal static partial class ExceptionLogger
{
    /// <summary>
    /// Logs an unhandled exception with associated HTTP context information.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="statusCode">The HTTP status code associated with the exception.</param>
    /// <param name="title">The title or summary of the error.</param>
    /// <param name="instance">The path where the exception occurred.</param>
    /// <param name="exception">The exception that was thrown. This parameter must always be last.</param>
    [LoggerMessage(
        EventId = 5000, 
        Level = LogLevel.Error, 
        Message = "Unhandled exception occurred. Status: {StatusCode}, Title: {Title}, Path: {Instance}")]
    public static partial void LogUnhandledException(
        this ILogger logger, 
        int statusCode, 
        string title, 
        string instance, 
        Exception exception);
}
