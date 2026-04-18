using Core.Domain.ResultPattern;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Core.Api.Extensions;

public static class ResultExtensions
{
    public static ProblemHttpResult ToProblemDetails(this Result result)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("Can't convert success result to problem.");
        }

        return CreateProblemDetails(result.ErrorDetails);
    }
    
    public static ProblemHttpResult ToProblemDetails<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("Can't convert success result to problem.");
        }

        return CreateProblemDetails(result.ErrorDetails);
    }

    private static ProblemHttpResult CreateProblemDetails(ErrorDetails error)
    {
        int statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        Dictionary<string, object?>? extensions = null;
        if (!string.IsNullOrWhiteSpace(error.Code))
        {
            extensions = new Dictionary<string, object?>
            {
                { "errorCode", error.Code } 
            };
        }
        
        return TypedResults.Problem(
            statusCode: statusCode,
            title: GetTitle(error.Type),
            detail: error.Message,
            extensions: extensions);
    }

    private static string GetTitle(ErrorType errorType) => errorType switch
    {
        ErrorType.Validation => "Bad Request",
        ErrorType.NotFound => "Not Found",
        ErrorType.Conflict => "Conflict",
        ErrorType.Unauthorized => "Unauthorized",
        ErrorType.Forbidden => "Forbidden",
        _ => "Internal Server Error"
    };
}
