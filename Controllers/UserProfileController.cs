// Controllers/UserProfileController.cs

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Berca_Backend.Data;
using Berca_Backend.Models;
using Berca_Backend.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Berca_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Requires authentication for all endpoints
    public class UserProfileController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<UserProfileController> _logger;

        public UserProfileController(AppDbContext context, IWebHostEnvironment environment, ILogger<UserProfileController> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        /// <summary>
        /// Get current user profile
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<UserProfileDto>>> GetCurrentUserProfile()
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.IsNullOrEmpty(username))
                {
                    return Unauthorized(new ApiResponse<UserProfileDto>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                var user = await _context.Users
                    .Include(u => u.UserProfile)
                    .FirstOrDefaultAsync(u => u.Username == username);

                if (user == null)
                {
                    return NotFound(new ApiResponse<UserProfileDto>
                    {
                        Success = false,
                        Message = "User not found"
                    });
                }

                // Create profile if doesn't exist
                if (user.UserProfile == null)
                {
                    user.UserProfile = new UserProfile
                    {
                        UserId = user.Id,
                        FullName = username, // Default to username
                        Email = $"{username}@tokoeniwan.com", // Default email
                        Gender = "Other", // Default gender
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.UserProfiles.Add(user.UserProfile);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Created default profile for user {username}");
                }

                var profileDto = new UserProfileDto
                {
                    Id = user.UserProfile.Id,
                    UserId = user.Id,
                    Username = user.Username,
                    Role = user.Role,
                    PhotoUrl = user.UserProfile.PhotoUrl,
                    FullName = user.UserProfile.FullName,
                    Gender = user.UserProfile.Gender,
                    Email = user.UserProfile.Email,
                    Department = user.UserProfile.Department,
                    Position = user.UserProfile.Position,
                    Division = user.UserProfile.Division,
                    PhoneNumber = user.UserProfile.PhoneNumber,
                    Bio = user.UserProfile.Bio,
                    CreatedAt = user.UserProfile.CreatedAt,
                    UpdatedAt = user.UserProfile.UpdatedAt
                };

                return Ok(new ApiResponse<UserProfileDto>
                {
                    Success = true,
                    Message = "Profile retrieved successfully",
                    Data = profileDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile");
                return StatusCode(500, new ApiResponse<UserProfileDto>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Update current user profile
        /// </summary>
        [HttpPut]
        public async Task<ActionResult<ApiResponse<UserProfileDto>>> UpdateProfile([FromBody] UpdateUserProfileDto updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<UserProfileDto>
                    {
                        Success = false,
                        Message = "Invalid data",
                        Errors = ModelState.SelectMany(x => x.Value?.Errors.Select(e => e.ErrorMessage) ?? new List<string>()).ToList()
                    });
                }

                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                var user = await _context.Users
                    .Include(u => u.UserProfile)
                    .FirstOrDefaultAsync(u => u.Username == username);

                if (user?.UserProfile == null)
                {
                    return NotFound(new ApiResponse<UserProfileDto>
                    {
                        Success = false,
                        Message = "User profile not found"
                    });
                }

                // Update profile fields
                user.UserProfile.FullName = updateDto.FullName;
                user.UserProfile.Gender = updateDto.Gender;
                user.UserProfile.Email = updateDto.Email;
                user.UserProfile.Department = updateDto.Department;
                user.UserProfile.Position = updateDto.Position;
                user.UserProfile.Division = updateDto.Division;
                user.UserProfile.PhoneNumber = updateDto.PhoneNumber;
                user.UserProfile.Bio = updateDto.Bio;
                user.UserProfile.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var updatedDto = new UserProfileDto
                {
                    Id = user.UserProfile.Id,
                    UserId = user.Id,
                    Username = user.Username,
                    Role = user.Role,
                    PhotoUrl = user.UserProfile.PhotoUrl,
                    FullName = user.UserProfile.FullName,
                    Gender = user.UserProfile.Gender,
                    Email = user.UserProfile.Email,
                    Department = user.UserProfile.Department,
                    Position = user.UserProfile.Position,
                    Division = user.UserProfile.Division,
                    PhoneNumber = user.UserProfile.PhoneNumber,
                    Bio = user.UserProfile.Bio,
                    CreatedAt = user.UserProfile.CreatedAt,
                    UpdatedAt = user.UserProfile.UpdatedAt
                };

                _logger.LogInformation($"Profile updated for user {username}");

                return Ok(new ApiResponse<UserProfileDto>
                {
                    Success = true,
                    Message = "Profile updated successfully",
                    Data = updatedDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile");
                return StatusCode(500, new ApiResponse<UserProfileDto>
                {
                    Success = false,
                    Message = "Failed to update profile"
                });
            }
        }

        /// <summary>
        /// Upload profile photo (max 500KB)
        /// </summary>
        [HttpPost("upload-photo")]
        public async Task<ActionResult<ApiResponse<string>>> UploadPhoto([FromForm] UploadPhotoDto uploadDto)
        {
            try
            {
                if (uploadDto.Photo == null || uploadDto.Photo.Length == 0)
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "No photo provided"
                    });
                }

                // Validate file size (max 500KB)
                const long maxSize = 500 * 1024; // 500KB
                if (uploadDto.Photo.Length > maxSize)
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Photo size must be less than 500KB"
                    });
                }

                // Validate file type
                var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
                if (!allowedTypes.Contains(uploadDto.Photo.ContentType.ToLower()))
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Only JPEG, PNG, and GIF files are allowed"
                    });
                }

                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                var user = await _context.Users
                    .Include(u => u.UserProfile)
                    .FirstOrDefaultAsync(u => u.Username == username);

                if (user?.UserProfile == null)
                {
                    return NotFound(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "User profile not found"
                    });
                }

                // Create uploads directory if it doesn't exist
                var uploadsDir = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, "uploads", "avatars");
                Directory.CreateDirectory(uploadsDir);

                // Generate unique filename
                var fileExtension = Path.GetExtension(uploadDto.Photo.FileName);
                var fileName = $"{username}_{DateTime.UtcNow:yyyyMMdd_HHmmss}{fileExtension}";
                var filePath = Path.Combine(uploadsDir, fileName);

                // Delete old photo if exists
                if (!string.IsNullOrEmpty(user.UserProfile.PhotoUrl))
                {
                    var oldFileName = Path.GetFileName(user.UserProfile.PhotoUrl);
                    var oldFilePath = Path.Combine(uploadsDir, oldFileName);
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                        _logger.LogInformation($"Deleted old photo: {oldFileName}");
                    }
                }

                // Save new photo
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await uploadDto.Photo.CopyToAsync(stream);
                }

                // Update database
                var photoUrl = $"/uploads/avatars/{fileName}";
                user.UserProfile.PhotoUrl = photoUrl;
                user.UserProfile.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Photo uploaded for user {username}: {fileName}");

                return Ok(new ApiResponse<string>
                {
                    Success = true,
                    Message = "Photo uploaded successfully",
                    Data = photoUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading photo");
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "Failed to upload photo"
                });
            }
        }

        /// <summary>
        /// Delete profile photo
        /// </summary>
        [HttpDelete("photo")]
        public async Task<ActionResult<ApiResponse<string>>> DeletePhoto()
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                var user = await _context.Users
                    .Include(u => u.UserProfile)
                    .FirstOrDefaultAsync(u => u.Username == username);

                if (user?.UserProfile == null)
                {
                    return NotFound(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "User profile not found"
                    });
                }

                // Delete file if exists
                if (!string.IsNullOrEmpty(user.UserProfile.PhotoUrl))
                {
                    var uploadsDir = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, "uploads", "avatars");
                    var fileName = Path.GetFileName(user.UserProfile.PhotoUrl);
                    var filePath = Path.Combine(uploadsDir, fileName);

                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                        _logger.LogInformation($"Deleted photo file: {fileName}");
                    }
                }

                // Update database
                user.UserProfile.PhotoUrl = null;
                user.UserProfile.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Photo deleted for user {username}");

                return Ok(new ApiResponse<string>
                {
                    Success = true,
                    Message = "Photo deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting photo");
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "Failed to delete photo"
                });
            }
        }
    }
}