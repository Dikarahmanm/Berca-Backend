// Services/AuthService.cs
using Berca_Backend.Data;
using Berca_Backend.Models;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace Berca_Backend.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;

        public AuthService(AppDbContext context)
        {
            _context = context;

            if (!_context.Users.Any())
            {
                var passwordHash = BCrypt.Net.BCrypt.HashPassword("123456");
                _context.Users.Add(new User { Username = "admin", PasswordHash = passwordHash });
                _context.SaveChanges();
            }
        }

        public async Task<User?> AuthenticateAsync(string username, string password)
        {
            var user = await _context.Users
                .Where(u => !u.IsDeleted) // ❗ Cek soft delete
                .FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return null;

            if (user == null || !user.IsActive) return null;

            bool isValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            return isValid ? user : null;
        }

        public async Task<bool> RegisterAsync(string username, string password)
        {
            if (await _context.Users.AnyAsync(u => u.Username == username)) return false;

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
            _context.Users.Add(new User
            {
                Username = username,
                PasswordHash = passwordHash,
                Role = "user",
                IsActive = true
            });

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
