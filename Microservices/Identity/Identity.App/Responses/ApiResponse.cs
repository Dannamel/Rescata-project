namespace Identity.App.Responses;

public class ApiResponse<T>
{
    public bool IsSuccess { get; init; }

    public T? Data { get; init; }

    public string? Message { get; init; }

    public IReadOnlyCollection<ApiError> Errors { get; init; } = [];

    public static ApiResponse<T> Ok(T data, string? message = null) => new()
    {
        IsSuccess = true,
        Data = data,
        Message = message
    };

    public static ApiResponse<T> Fail(IReadOnlyCollection<ApiError> errors, string? message = null) => new()
    {
        IsSuccess = false,
        Message = message,
        Errors = errors
    };
}
