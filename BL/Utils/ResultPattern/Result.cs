namespace BL.Utils.ResultPattern;

public class Result
{
    public bool IsSuccess { get; set; }
    public bool IsFailiure => !IsSuccess;
    private IEnumerable<string> Errors { get; set; } = [];
    public string Message => IsFailiure ? string.Join(Environment.NewLine, Errors) : string.Empty;

    public static Result Fail(IEnumerable<string>? errors = null)
    {
        return new Result
        {
            IsSuccess = false,
            Errors = errors ?? Array.Empty<string>()
        };
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