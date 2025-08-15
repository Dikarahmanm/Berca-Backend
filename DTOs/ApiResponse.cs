using System.ComponentModel.DataAnnotations;

namespace Berca_Backend.DTOs
{
    /// <summary>
    /// Generic API response wrapper
    /// </summary>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public List<string>? Errors { get; set; }

        /// <summary>
        /// Create a successful response with data
        /// </summary>
        public static ApiResponse<T> SuccessResponse(T data, string message = "Success")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data,
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Create an error response with optional error list
        /// </summary>
        public static ApiResponse<T> ErrorResponse(string message, List<string>? errors = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Data = default(T),
                Errors = errors,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Non-generic API response for simple operations
    /// </summary>
    public class ApiResponse : ApiResponse<object>
    {
    }
}