using Microsoft.Extensions.Logging;

namespace Core.Api;

public static partial class ProblemDetailsLogger
{
    // Указываем уровень, ID события и шаблон сообщения
    [LoggerMessage(
        EventId = 1001, 
        Level = LogLevel.Error, 
        Message = "Title: {Title}, Status: {Status}, Detail: {Detail}, Instance: {Instance}")]
    public static partial void LogProblemDetails(
        this ILogger logger, 
        string? title, 
        int? status, 
        string? detail, 
        string? instance, 
        Exception exception); // Exception всегда идет последним параметром
}
