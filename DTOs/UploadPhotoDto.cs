using System.ComponentModel.DataAnnotations;

namespace Berca_Backend.DTOs
{
    public class UploadPhotoDto
    {
        [Required(ErrorMessage = "File foto wajib dipilih")]
        public IFormFile Photo { get; set; } = null!;
    }
}