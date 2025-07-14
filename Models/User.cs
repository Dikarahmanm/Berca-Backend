namespace Berca_Backend.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;

        public string Role { get; set; } = "user"; // default role
        public bool IsActive { get; set; } = true; // default aktif
        public bool IsDeleted { get; set; } = false;
    }

}