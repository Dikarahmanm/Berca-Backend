﻿using Berca_Backend.Data;
using Berca_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using System.Security.Claims; // ✅ NEW: Add for ClaimTypes

namespace Berca_Backend.Controllers
{
    [ApiController]
    [Route("admin")]
    [Authorize(Roles = "Admin,Manager")] // ✅ UPDATED: Both Admin and Manager can access
    public class UserAdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UserAdminController> _logger;

        public UserAdminController(AppDbContext context, ILogger<UserAdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ✅ UPDATED: Helper method to check permissions with better debugging
        private bool CanPerformAction(string action)
        {
            var userRole = User.FindFirst("role")?.Value ??
                          User.FindFirst(ClaimTypes.Role)?.Value ??
                          User.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value ?? "";

            var username = User.Identity?.Name ?? "unknown";
            var isAuthenticated = User.Identity?.IsAuthenticated ?? false;

            _logger.LogInformation("🔐 Permission check - Authenticated: {Auth}, Username: {User}, Role: {Role}, Action: {Action}",
                isAuthenticated, username, userRole, action);

            // ✅ NEW: Debug all claims
            if (User.Claims.Any())
            {
                _logger.LogInformation("📋 Available claims:");
                foreach (var claim in User.Claims)
                {
                    _logger.LogInformation("- {Type}: {Value}", claim.Type, claim.Value);
                }
            }
            else
            {
                _logger.LogWarning("⚠️ No claims found in User.Claims");
            }

            if (!isAuthenticated)
            {
                _logger.LogWarning("❌ User is not authenticated for action: {Action}", action);
                return false;
            }

            if (string.IsNullOrEmpty(userRole))
            {
                _logger.LogWarning("❌ No role found for user {Username} for action: {Action}", username, action);
                return false;
            }

            return action switch
            {
                "ChangeRole" => userRole == "Manager",
                "ToggleActive" => userRole == "Admin" || userRole == "Manager",
                "DeleteUser" => userRole == "Manager",
                "RestoreUser" => userRole == "Manager",
                _ => false
            };
        }

        // 1. List semua user dengan pagination dan search
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null)
        {
            _logger.LogInformation("GET /admin/users called - page={Page}, pageSize={PageSize}, search={Search}", page, pageSize, search);

            // 🔍 DEBUG: Check all users first
            var allUsers = await _context.Users.ToListAsync();
            var deletedCount = allUsers.Count(u => u.IsDeleted);
            var activeCount = allUsers.Count(u => !u.IsDeleted);

            _logger.LogInformation("🔍 DEBUG: Total users in DB: {Total}, Deleted: {Deleted}, Active: {Active}",
                allUsers.Count, deletedCount, activeCount);

            // ✅ CRITICAL: Filter out deleted users
            var query = _context.Users.Where(u => !u.IsDeleted).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u =>
                    u.Username.Contains(search) ||
                    u.Role.Contains(search));
            }

            var total = await query.CountAsync();
            var users = await query
                .OrderBy(u => u.Username)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Role,
                    u.IsActive,
                    u.IsDeleted // 🔍 Add for debugging
                })
                .ToListAsync();

            _logger.LogInformation("✅ Returning {Count} active users from total {Total}", users.Count, total);

            return Ok(new { total, users });
        }

        // 2. Update role dan status aktif user
        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            var adminUsername = User.Identity?.Name ?? "unknown-admin";
            var userRole = User.FindFirst("role")?.Value ?? "";
            var changes = new List<string>();

            // ✅ NEW: Check role change permission
            if (user.Role != request.Role && request.Role != null)
            {
                if (!CanPerformAction("ChangeRole"))
                {
                    return Forbid($"User with role '{userRole}' cannot change user roles");
                }

                // ✅ NEW: Validate allowed roles for Manager
                if (userRole == "Manager" && !new[] { "User", "Admin" }.Contains(request.Role))
                {
                    return BadRequest($"Manager can only set roles to User or Admin");
                }

                changes.Add($"Role: {user.Role} → {request.Role}");
                user.Role = request.Role;
            }

            // ✅ NEW: Check active status change permission
            if (user.IsActive != request.IsActive)
            {
                if (!CanPerformAction("ToggleActive"))
                {
                    return Forbid($"User with role '{userRole}' cannot change user active status");
                }

                changes.Add($"IsActive: {user.IsActive} → {request.IsActive}");
                user.IsActive = request.IsActive;
            }

            await _context.SaveChangesAsync();

            if (changes.Any())
            {
                var action = $"Updated user {user.Username} (ID: {user.Id}): " + string.Join(", ", changes);
                await _context.LogActivityAsync(adminUsername, action);
            }

            return Ok(new { message = "User updated successfully" });
        }

        // 3. Hapus user
        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            // ✅ NEW: Check delete permission
            if (!CanPerformAction("DeleteUser"))
            {
                var userRole = User.FindFirst("role")?.Value ?? "";
                return Forbid($"User with role '{userRole}' cannot delete users");
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { message = "User not found" });

            // Check if user is already deleted
            if (user.IsDeleted)
                return BadRequest(new { message = "User already deleted" });

            // Soft delete
            user.IsDeleted = true;
            await _context.SaveChangesAsync();

            var adminUsername = User.Identity?.Name ?? "unknown-admin";
            await _context.LogActivityAsync(adminUsername, $"Soft deleted user {user.Username} (ID: {user.Id})");

            return Ok(new { message = "User deleted successfully" });
        }

        // GET /admin/logs?page=1&pageSize=10&search=admin
        [HttpGet("logs")]
        public async Task<IActionResult> GetLogs(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            var query = _context.LogActivities.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(l => l.Username.Contains(search));
            }

            if (from.HasValue)
            {
                query = query.Where(l => l.Timestamp >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(l => l.Timestamp <= to.Value);
            }

            var totalCount = await query.CountAsync();
            var logs = await query
                .OrderByDescending(l => l.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                Total = totalCount,
                Page = page,
                PageSize = pageSize,
                Data = logs
            });
        }

        [HttpGet("logs/export")]
        public IActionResult ExportLogsToXlsx([FromQuery] string? search, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var query = _context.LogActivities.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(l => l.Username.Contains(search) || l.Action.Contains(search));
            }

            if (from.HasValue)
                query = query.Where(l => l.Timestamp >= from.Value);

            if (to.HasValue)
                query = query.Where(l => l.Timestamp <= to.Value);

            var logs = query.OrderByDescending(l => l.Timestamp).ToList();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Logs");
            worksheet.Cell(1, 1).Value = "Username";
            worksheet.Cell(1, 2).Value = "Action";
            worksheet.Cell(1, 3).Value = "Timestamp";

            for (int i = 0; i < logs.Count; i++)
            {
                worksheet.Cell(i + 2, 1).Value = logs[i].Username;
                worksheet.Cell(i + 2, 2).Value = logs[i].Action;
                worksheet.Cell(i + 2, 3).Value = logs[i].Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"LogActivity_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpPut("users/{id}/restore")]
        public async Task<IActionResult> RestoreUser(int id)
        {
            // ✅ NEW: Check restore permission
            if (!CanPerformAction("RestoreUser"))
            {
                var userRole = User.FindFirst("role")?.Value ?? "";
                return Forbid($"User with role '{userRole}' cannot restore users");
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { message = "User not found" });

            if (!user.IsDeleted)
                return BadRequest(new { message = "User is not deleted" });

            user.IsDeleted = false;
            await _context.SaveChangesAsync();

            var adminUsername = User.Identity?.Name ?? "unknown-admin";
            await _context.LogActivityAsync(adminUsername, $"Restored user {user.Username} (ID: {user.Id})");

            _logger.LogInformation("✅ User {Username} (ID: {Id}) restored", user.Username, user.Id);

            return Ok(new { message = "User restored successfully" });
        }

        // Get deleted users
        [HttpGet("users/deleted")]
        public async Task<IActionResult> GetDeletedUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            _logger.LogInformation("GET /admin/users/deleted called - page={Page}, pageSize={PageSize}", page, pageSize);

            var query = _context.Users.Where(u => u.IsDeleted).AsQueryable();

            var total = await query.CountAsync();
            var users = await query
                .OrderByDescending(u => u.Id) // Most recently deleted first
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Role,
                    u.IsActive,
                    u.IsDeleted
                })
                .ToListAsync();

            _logger.LogInformation("✅ Returning {Count} deleted users from total {Total}", users.Count, total);

            return Ok(new { total, users });
        }

        // ✅ NEW: Debug authentication endpoint
        [HttpGet("debug-auth")]
        [AllowAnonymous] // Allow access to debug auth issues
        public IActionResult DebugAuth()
        {
            var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
            var username = User.Identity?.Name ?? "Not found";
            var authType = User.Identity?.AuthenticationType ?? "Not found";

            var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            var cookies = Request.Cookies.Select(c => new { c.Key, HasValue = !string.IsNullOrEmpty(c.Value) }).ToList();

            var debugInfo = new
            {
                IsAuthenticated = isAuthenticated,
                Username = username,
                AuthenticationType = authType,
                Claims = claims,
                Cookies = cookies,
                Headers = Request.Headers.Select(h => new { h.Key, Values = h.Value.ToArray() }).ToList()
            };

            _logger.LogInformation("🔍 Auth Debug - Authenticated: {Auth}, User: {User}, Claims: {ClaimCount}",
                isAuthenticated, username, claims.Count);

            return Ok(debugInfo);
        }

        // ✅ NEW: Get current user permissions (for frontend)
        [HttpGet("permissions")]
        public IActionResult GetPermissions()
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var userRole = User.FindFirst("role")?.Value ??
                          User.FindFirst(ClaimTypes.Role)?.Value ??
                          User.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value ?? "";
            var username = User.Identity?.Name ?? "";

            var permissions = new
            {
                Role = userRole,
                Username = username,
                CanChangeRole = CanPerformAction("ChangeRole"),
                CanToggleActive = CanPerformAction("ToggleActive"),
                CanDeleteUser = CanPerformAction("DeleteUser"),
                CanRestoreUser = CanPerformAction("RestoreUser"),
                AllowedRoles = userRole == "Manager" ? new[] { "User", "Admin" } : new string[0]
            };

            return Ok(permissions);
        }
    }

    // DTO untuk update user
    public class UpdateUserRequest
    {
        public string? Role { get; set; }
        public bool IsActive { get; set; }
    }
}