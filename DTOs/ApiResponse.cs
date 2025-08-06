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
    }

    /// <summary>
    /// Non-generic API response for simple operations
    /// </summary>
    public class ApiResponse : ApiResponse<object>
    {
    }
}