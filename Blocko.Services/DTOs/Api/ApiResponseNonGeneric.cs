using System.Collections.Generic;

namespace Blocko.Services.DTOs.Api
{
    public class ApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<string> Errors { get; set; }

        public ApiResponse()
        {
            Message = string.Empty;
            Errors = new List<string>();
        }

        public static ApiResponse Ok(string message = "Success")
        {
            return new ApiResponse { Success = true, Message = message };
        }

        public static ApiResponse Error(string message, List<string> errors = null)
        {
            return new ApiResponse { Success = false, Message = message, Errors = errors ?? new List<string>() };
        }
    }
}
