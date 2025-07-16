using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Berca_Backend.Models;
using Berca_Backend.Services;
using Berca_Backend.Data;

namespace Berca_Backend.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly AppDbContext _context; // Tambahkan context untuk akses log

        public AuthController(IAuthService authService, AppDbContext context)
        {
            _authService = authService;
            _context = context;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _authService.AuthenticateAsync(request.Username, request.Password);
            if (user == null)
                return Unauthorized(new { message = "Invalid credentials" });

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.Role, user.Role ?? "User"),
        new Claim("UserId", user.Id.ToString()) // Tambah UserId juga
    };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            // 🔍 Debug: Log sebelum SignIn
            Console.WriteLine($"🔐 Attempting SignIn for user: {user.Username}");
            Console.WriteLine($"🔐 Claims count: {claims.Count}");

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            // 🔍 Debug: Log setelah SignIn
            Console.WriteLine($"✅ SignIn completed for user: {user.Username}");
            Console.WriteLine($"🍪 Response cookies count: {HttpContext.Response.Cookies}");

            // Log login activity
            await _context.LogActivityAsync(user.Username, $"User {user.Username} logged in");

            return Ok(new
            {
                message = "Login successful",
                user = user.Username,
                role = user.Role,
                success = true
            });
        }

        // Tambah endpoint untuk debug auth status
        [HttpGet("debug-auth")]
        public IActionResult DebugAuth()
        {
            var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
            var username = User.Identity?.Name;
            var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            var allClaims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();

            Console.WriteLine($"🔍 Debug Auth - IsAuthenticated: {isAuthenticated}");
            Console.WriteLine($"🔍 Debug Auth - Username: {username}");
            Console.WriteLine($"🔍 Debug Auth - Role: {role}");

            return Ok(new
            {
                isAuthenticated = isAuthenticated,
                username = username,
                role = role,
                claims = allClaims,
                cookieCount = Request.Cookies.Count,
                cookies = Request.Cookies.Keys.ToList()
            });
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var username = User.Identity?.Name ?? "Unknown";

            // 🔍 Log logout
            await _context.LogActivityAsync(username, $"User {username} logged out");

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok(new { message = "Logout successful" });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            bool success = await _authService.RegisterAsync(request.Username, request.Password);
            if (!success)
                return BadRequest(new { message = "Username already exists" });

            // 🔍 Log register
            await _context.LogActivityAsync(request.Username, $"User {request.Username} registered");

            return Ok(new { message = "User registered successfully" });
        }

        [HttpGet("check")]
        public IActionResult CheckAuth()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return Ok(new { Username = User.Identity.Name });
            }
            return Unauthorized();
        }

        [HttpGet("whoami")]
        public IActionResult WhoAmI()
        {
            return Ok(new
            {
                Username = User.Identity?.Name,
                Role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value
            });
        }
    }
}
