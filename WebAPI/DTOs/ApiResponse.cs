namespace WebAPI.DTOs
{
    public class ApiResponse<T>
    {
        public int StatusCode { get; set; }
        public string Message { get; set; } = "";
        public T? Data { get; set; }
        public static ApiResponse<T> Success(T? data, string message = "Success", int code = 200)
        {
            return new ApiResponse<T> { StatusCode = code, Message = message, Data = data };
        }
        public static ApiResponse<T> Fail(string message, int code = 400)
        {
            return new ApiResponse<T> { StatusCode = code, Message = message };
        }
    }
}
