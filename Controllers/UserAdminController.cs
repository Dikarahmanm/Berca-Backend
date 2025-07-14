using Berca_Backend.Data;
using Berca_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;

namespace Berca_Backend.Controllers
{
    [ApiController]
    [Route("admin")]
    [Authorize(Roles = "Admin")] // Hanya admin yang bisa akses
    public class UserAdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UserAdminController> _logger;

        public UserAdminController(AppDbContext context, ILogger<UserAdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // 1. List semua user dengan pagination dan search
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null)
        {
            _logger.LogInformation("GET /admin/users dipanggil dengan page={Page}, pageSize={PageSize}, search={Search}", page, pageSize, search);

            var query = _context.Users.AsQueryable();

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
                })
                .ToListAsync();

            _logger.LogInformation("Mengembalikan {Count} user dari total {Total}", users.Count, total);

            return Ok(new { total, users });
        }

        // 2. Update role dan status aktif user
        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            var adminUsername = User.Identity?.Name ?? "unknown-admin";
            var changes = new List<string>();

            if (user.Role != request.Role && request.Role != null)
            {
                changes.Add($"Role: {user.Role} → {request.Role}");
                user.Role = request.Role;
            }

            if (user.IsActive != request.IsActive)
            {
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
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.IsDeleted = true;
            await _context.SaveChangesAsync();

            var adminUsername = User.Identity?.Name ?? "unknown-admin";
            await _context.LogActivityAsync(adminUsername, $"Soft deleted user {user.Username} (ID: {user.Id})");

            return Ok(new { message = "User deleted (soft)" });
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


    }

    // DTO untuk update user
    public class UpdateUserRequest
    {
        public string? Role { get; set; }
        public bool IsActive { get; set; }
    }
}
