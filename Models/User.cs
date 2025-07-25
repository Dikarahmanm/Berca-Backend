using System.ComponentModel.DataAnnotations;

namespace Berca_Backend.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Role { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;

        // ✅ ADD Navigation property to UserProfile
        public virtual UserProfile? UserProfile { get; set; }
    }
}