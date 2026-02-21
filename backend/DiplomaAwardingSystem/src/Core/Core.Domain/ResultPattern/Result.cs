using System.Diagnostics;

namespace Core.Domain.ResultPattern;

#pragma warning disable CA2225

[DebuggerDisplay("{IsSuccess ? \"Success\" : \"Failure: \" + ErrorDetails.Code}")]
public readonly struct Result
{
    public ErrorDetails ErrorDetails { get; }
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    internal Result(bool isSuccess, ErrorDetails errorDetails)
    {
        if (isSuccess && errorDetails != ErrorDetails.None)
            throw new InvalidOperationException("Success result cannot have an error.");

        if (!isSuccess && errorDetails == ErrorDetails.None)
            throw new InvalidOperationException("Failure result must have an error.");

        IsSuccess = isSuccess;
        ErrorDetails = errorDetails;
    }

    public static Result Success() => new(true, ErrorDetails.None);
    public static Result Failure(ErrorDetails errorDetails) => new(false, errorDetails);

    public static Result<T> Success<T>(T value) => new(value, true, ErrorDetails.None);
    public static Result<T> Failure<T>(ErrorDetails errorDetails) => new(default, false, errorDetails);

    public static implicit operator Result(ErrorDetails errorDetails) => Failure(errorDetails);

    public TResult Match<TResult>(Func<TResult> onSuccess, Func<ErrorDetails, TResult> onFailure)
        => IsSuccess ? onSuccess() : onFailure(ErrorDetails);
}

[DebuggerDisplay("{IsSuccess ? \"Success\" : \"Failure: \" + ErrorDetails.Code}")]
public readonly struct Result<T>
{
    public T? Value { get; }
    public ErrorDetails ErrorDetails { get; }
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    internal Result(T? value, bool isSuccess, ErrorDetails errorDetails)
    {
        if (isSuccess && errorDetails != ErrorDetails.None)
            throw new InvalidOperationException("Success result cannot have an error.");

        if (!isSuccess && errorDetails == ErrorDetails.None)
            throw new InvalidOperationException("Failure result must have an error.");

        Value = value;
        IsSuccess = isSuccess;
        ErrorDetails = errorDetails;
    }

    public static implicit operator Result<T>(T value) => Result.Success(value);
    public static implicit operator Result<T>(ErrorDetails errorDetails) => Result.Failure<T>(errorDetails);

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<ErrorDetails, TResult> onFailure)
        => IsSuccess ? onSuccess(Value!) : onFailure(ErrorDetails);
}

public readonly record struct ErrorDetails(string Code, string Message)
{
    public static readonly ErrorDetails None = new(string.Empty, string.Empty);
}
