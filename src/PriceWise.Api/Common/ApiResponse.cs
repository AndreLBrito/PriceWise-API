namespace PriceWise.Api.Common;

public sealed record ApiResponse<T>(bool Success, T? Data, ApiError? Error)
{
    public static ApiResponse<T> Ok(T data) => new(true, data, null);

    public static ApiResponse<T> Fail(string code, string message, int? statusCode = null) =>
        new(false, default, ApiError.Create(code, message, statusCode));
}
