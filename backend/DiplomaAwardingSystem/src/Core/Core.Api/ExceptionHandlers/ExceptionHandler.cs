using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Core.Api.ExceptionHandlers;

/// <summary>
/// Handles unhandled exceptions in the application, maps them to appropriate HTTP status codes,
/// logs the errors, and returns standardized problem details to clients.
/// </summary>
public sealed class ExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ExceptionHandler> logger) : IExceptionHandler
{
    /// <summary>
    /// Attempts to handle an exception by mapping it to an HTTP status code, logging it,
    /// and writing a standardized problem details response to the client.
    /// </summary>
    /// <param name="httpContext">The HTTP context for the current request.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation witch containing a boolean indicating whether the exception was handled.</returns>
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        (int statusCode, string title, string clientDetail) = exception switch
        {
            OperationCanceledException => 
                (StatusCodes.Status499ClientClosedRequest, "Client Closed Request", "The request was canceled by the client."),
            
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error", "An unexpected problem occurred.")
        };

        logger.LogUnhandledException(statusCode, title, httpContext.Request.Path, exception);

        ProblemDetails problemDetails = new()
        {
            Title = title,
            Status = statusCode,
            Detail = clientDetail,
            Instance = httpContext.Request.Path
        };
        
        httpContext.Response.StatusCode = statusCode;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });
    }
}
