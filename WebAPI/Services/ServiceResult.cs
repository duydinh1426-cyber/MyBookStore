namespace WebAPI.Services
{
    public class ServiceResult
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public int StatusCode { get; set; }

        public static ServiceResult Success(string? message = null, int code = 200)
            => new() { IsSuccess = true, Message = message, StatusCode = code };

        public static ServiceResult Failure(string message, int code = 400)
            => new() { IsSuccess = false, Message = message, StatusCode = code };
    }
    public class ServiceResult<T> : ServiceResult
    {
        public T? Data { get; set; }

        public static ServiceResult<T> Success(T data, string? message = null, int code = 200) 
            => new() { IsSuccess = true, Data = data, Message = message, StatusCode = code };
        
        public static new ServiceResult<T> Failure(string message, int code = 400)
            => new() { IsSuccess = false, Message = message, StatusCode = code };

    }
}
