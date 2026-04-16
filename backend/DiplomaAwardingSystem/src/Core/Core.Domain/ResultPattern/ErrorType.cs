namespace Core.Domain.ResultPattern;

public enum ErrorType
{
    Failure = 0,       // server error (500)
    Validation = 1,    // validation and business logic error (400)
    NotFound = 2,      // not found (404)
    Conflict = 3,      // condition conflict (409)
    Unauthorized = 4,  // authorization error (401)
    Forbidden = 5,     // not allowed (403)
}
