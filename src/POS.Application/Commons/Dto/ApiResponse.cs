namespace POS.Application.Common.Dto
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public static ApiResponse<T> Ok(T data, string message = "")
            => new() { Success = true, Data = data, Message = message };

        public static ApiResponse<T> Created(T data, string message = "")
            => new() { Success = true, Data = data, Message = message };

        public static ApiResponse<T> BadRequest(string message)
            => new() { Success = false, Message = message };
        public static ApiResponse<T> Unauthorized(string message)
=> new() { Success = false, Message = message };

        public static ApiResponse<T> Forbidden(string message)
            => new() { Success = false, Message = message };

        public static ApiResponse<T> NotFound(string message)
            => new() { Success = false, Message = message };
    }

    public class ApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        public static ApiResponse Ok(string message = "")
            => new() { Success = true, Message = message };

        public static ApiResponse BadRequest(string message)
            => new() { Success = false, Message = message };

        public static ApiResponse NotFound(string message)
            => new() { Success = false, Message = message };
        public static ApiResponse Created(string message = "")
=> new() { Success = true, Message = message };
        public static ApiResponse Unauthorized(string message)
   => new() { Success = false, Message = message };

        public static ApiResponse Forbidden(string message)
            => new() { Success = false, Message = message };
    }
}