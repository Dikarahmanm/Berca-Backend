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

            // Initialize admin user if no users exist
            if (!_context.Users.Any())
            {
                var passwordHash = BCrypt.Net.BCrypt.HashPassword("123456");
                _context.Users.Add(new User
                {
                    Username = "admin",
                    PasswordHash = passwordHash,
                    Role = "Admin",
                    IsActive = true,
                    IsDeleted = false
                });
                _context.SaveChanges();
            }
        }

        public async Task<User?> AuthenticateAsync(string username, string password)
        {
            // ✅ STEP 1: Find user by username (include all fields for validation)
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            // ✅ STEP 2: Check if user exists
            if (user == null)
            {
                // Log failed attempt for non-existent user
                Console.WriteLine($"❌ Authentication failed: User '{username}' not found");
                return null;
            }

            // ✅ STEP 3: Check if user is deleted (soft delete validation)
            if (user.IsDeleted)
            {
                Console.WriteLine($"❌ Authentication failed: User '{username}' is deleted");
                await _context.LogActivityAsync(username, $"Login attempt blocked: User {username} is deleted");
                return null;
            }

            // ✅ STEP 4: Check if user is active
            if (!user.IsActive)
            {
                Console.WriteLine($"❌ Authentication failed: User '{username}' is inactive");
                await _context.LogActivityAsync(username, $"Login attempt blocked: User {username} is inactive");
                return null;
            }

            // ✅ STEP 5: Verify password
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            if (!isPasswordValid)
            {
                Console.WriteLine($"❌ Authentication failed: Invalid password for user '{username}'");
                await _context.LogActivityAsync(username, $"Login attempt failed: Invalid password for user {username}");
                return null;
            }

            // ✅ STEP 6: All validations passed
            Console.WriteLine($"✅ Authentication successful for user: {username}");
            return user;
        }

        public async Task<bool> RegisterAsync(string username, string password)
        {
            // Check if username already exists (including deleted users)
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (existingUser != null)
            {
                // If user exists but is deleted, we could optionally restore them
                if (existingUser.IsDeleted)
                {
                    Console.WriteLine($"⚠️ Registration failed: Username '{username}' exists but is deleted");
                    // Optionally: You could add logic here to restore the user instead
                    return false;
                }

                Console.WriteLine($"❌ Registration failed: Username '{username}' already exists");
                return false;
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
            var newUser = new User
            {
                Username = username,
                PasswordHash = passwordHash,
                Role = "User", // Default role
                IsActive = true, // New users are active by default
                IsDeleted = false // New users are not deleted
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            Console.WriteLine($"✅ User '{username}' registered successfully");
            await _context.LogActivityAsync(username, $"User {username} registered successfully");

            return true;
        }

        // ✅ NEW: Method to get user status for validation
        public async Task<UserValidationResult> ValidateUserAsync(string username)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                return new UserValidationResult
                {
                    IsValid = false,
                    Reason = "User not found",
                    StatusCode = "USER_NOT_FOUND"
                };
            }

            if (user.IsDeleted)
            {
                return new UserValidationResult
                {
                    IsValid = false,
                    Reason = "User account has been deleted",
                    StatusCode = "USER_DELETED"
                };
            }

            if (!user.IsActive)
            {
                return new UserValidationResult
                {
                    IsValid = false,
                    Reason = "User account is inactive",
                    StatusCode = "USER_INACTIVE"
                };
            }

            return new UserValidationResult
            {
                IsValid = true,
                Reason = "User is valid",
                StatusCode = "USER_VALID",
                User = user
            };
        }
    }

    // ✅ NEW: Validation result model
    public class UserValidationResult
    {
        public bool IsValid { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string StatusCode { get; set; } = string.Empty;
        public User? User { get; set; }
    }
}