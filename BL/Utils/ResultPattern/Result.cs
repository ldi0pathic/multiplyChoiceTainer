namespace BL.Utils.ResultPattern;

public class Result
{
    public bool IsSuccess { get; private set; }
    public bool IsFailure => !IsSuccess;
    private IEnumerable<string> Errors { get; set; } = Array.Empty<string>();
    public string Message => IsFailure ? string.Join(Environment.NewLine, Errors) : string.Empty;

    public static Result Fail(IEnumerable<string>? errors = null)
    {
        return new Result
        {
            IsSuccess = false,
            Errors = errors ?? Array.Empty<string>()
        };
    }

    public static Result Fail(string? error = null)
    {
        return Fail([error ?? string.Empty]);
    }

    public static Result Exception(Exception? exception = null)
    {
        return new Result
        {
            IsSuccess = false,
            Errors = exception != null ? new[] { exception.Message } : Array.Empty<string>()
        };
    }

    public static Result Success()
    {
        return new Result
        {
            IsSuccess = true
        };
    }
}

public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public bool IsFailure => !IsSuccess;
    private IEnumerable<string> Errors { get; set; } = Array.Empty<string>();
    public string Message => IsFailure ? string.Join(Environment.NewLine, Errors) : string.Empty;
    public T? Value { get; private set; }

    public static Result<T> Fail(IEnumerable<string>? errors = null)
    {
        return new Result<T>
        {
            IsSuccess = false,
            Errors = errors ?? Array.Empty<string>()
        };
    }

    public static Result<T> Fail(string error)
    {
        return Fail([error]);
    }

    public static Result<T> Exception(Exception? exception = null)
    {
        return new Result<T>
        {
            IsSuccess = false,
            Errors = exception != null ? new[] { exception.Message } : Array.Empty<string>()
        };
    }

    public static Result<T> Success(T value)
    {
        return new Result<T>
        {
            IsSuccess = true,
            Value = value
        };
    }
}