using System.ComponentModel.DataAnnotations;

namespace Berca_Backend.DTOs
{
    public class UpdateUserProfileDto
    {
        [Required(ErrorMessage = "Nama lengkap wajib diisi")]
        [StringLength(100, ErrorMessage = "Nama lengkap maksimal 100 karakter")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Jenis kelamin wajib dipilih")]
        [StringLength(10)]
        public string Gender { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email wajib diisi")]
        [EmailAddress(ErrorMessage = "Format email tidak valid")]
        [StringLength(100, ErrorMessage = "Email maksimal 100 karakter")]
        public string Email { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Departemen maksimal 50 karakter")]
        public string? Department { get; set; }

        [StringLength(50, ErrorMessage = "Jabatan maksimal 50 karakter")]
        public string? Position { get; set; }

        [StringLength(50, ErrorMessage = "Divisi maksimal 50 karakter")]
        public string? Division { get; set; }

        [StringLength(20, ErrorMessage = "Nomor telepon maksimal 20 karakter")]
        public string? PhoneNumber { get; set; }

        [StringLength(500, ErrorMessage = "Bio maksimal 500 karakter")]
        public string? Bio { get; set; }
    }
}