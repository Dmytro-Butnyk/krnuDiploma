using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Core.Api.ExceptionHandlers;

public sealed class ExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        (int statusCode, string title, string clientDetail) = exception switch
        {
            OperationCanceledException => 
                (StatusCodes.Status499ClientClosedRequest, "Client Closed Request", "The request was canceled by the client."),
            
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error", "An unexpected problem occurred.")
        };

        logger.LogError(
            exception,
            "Unhandled exception occurred. Status: {StatusCode}, Title: {Title}, Path: {Instance}",
            statusCode, title, httpContext.Request.Path);

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
