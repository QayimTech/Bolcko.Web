using System.Collections.Generic;

namespace Blocko.Services.DTOs.Api
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public List<string> Errors { get; set; }

        public ApiResponse()
        {
            Message = string.Empty;
            Data = default!;
            Errors = new List<string>();
        }

        public static ApiResponse<T> Ok(T data, string message = "Success")
        {
            return new ApiResponse<T> { Success = true, Message = message, Data = data };
        }

        public static ApiResponse<T> Error(string message, List<string> errors = null)
        {
            return new ApiResponse<T> { Success = false, Message = message, Errors = errors ?? new List<string>() };
        }
    }
}
