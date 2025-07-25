using System.ComponentModel.DataAnnotations;

namespace Berca_Backend.DTOs
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    // Generic response tanpa data
    public class ApiResponse : ApiResponse<object?>
    {
    }
}