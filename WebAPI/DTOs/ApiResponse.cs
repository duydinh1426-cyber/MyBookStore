namespace WebAPI.Common;

public class ApiResponse<T> // trả kèm data
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int StatusCode { get; set; } = 200;
    public T? Data { get; set; }

    public static ApiResponse<T> Ok(T data, string? message = null) =>
        new() { Success = true, Message = message, StatusCode = 200, Data = data };

    public static ApiResponse<T> Fail(string message, int statusCode = 400) =>
        new() { Success = false, Message = message, StatusCode = statusCode };
}