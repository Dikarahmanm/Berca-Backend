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
        private readonly AppDbContext _context;

        public AuthController(IAuthService authService, AppDbContext context)
        {
            _authService = authService;
            _context = context;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // ✅ STEP 1: Validate input
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Username and password are required",
                    errorCode = "INVALID_INPUT"
                });
            }

            // ✅ STEP 2: Pre-validate user status (optional - for better error messages)
            if (_authService is AuthService authServiceImpl)
            {
                var validationResult = await authServiceImpl.ValidateUserAsync(request.Username);
                if (!validationResult.IsValid)
                {
                    // Return specific error messages based on validation
                    var errorResponse = validationResult.StatusCode switch
                    {
                        "USER_NOT_FOUND" => new
                        {
                            success = false,
                            message = "Invalid username or password",
                            errorCode = "INVALID_CREDENTIALS"
                        },
                        "USER_DELETED" => new
                        {
                            success = false,
                            message = "This account has been deleted. Please contact administrator.",
                            errorCode = "ACCOUNT_DELETED"
                        },
                        "USER_INACTIVE" => new
                        {
                            success = false,
                            message = "This account is inactive. Please contact administrator.",
                            errorCode = "ACCOUNT_INACTIVE"
                        },
                        _ => new
                        {
                            success = false,
                            message = "Login failed",
                            errorCode = "LOGIN_FAILED"
                        }
                    };

                    // Don't reveal too much information for security reasons
                    // For USER_NOT_FOUND, we return generic "Invalid credentials"
                    return Unauthorized(errorResponse);
                }
            }

            // ✅ STEP 3: Attempt authentication
            var user = await _authService.AuthenticateAsync(request.Username, request.Password);

            if (user == null)
            {
                // ✅ Generic error for security (don't reveal if user exists or not)
                await _context.LogActivityAsync(request.Username, $"Failed login attempt for username: {request.Username}");

                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid username or password",
                    errorCode = "INVALID_CREDENTIALS"
                });
            }

            // ✅ STEP 4: Double-check user status (redundant but safe)
            if (user.IsDeleted)
            {
                await _context.LogActivityAsync(user.Username, $"Login blocked: User {user.Username} is deleted");
                return Unauthorized(new
                {
                    success = false,
                    message = "This account has been deleted. Please contact administrator.",
                    errorCode = "ACCOUNT_DELETED"
                });
            }

            if (!user.IsActive)
            {
                await _context.LogActivityAsync(user.Username, $"Login blocked: User {user.Username} is inactive");
                return Unauthorized(new
                {
                    success = false,
                    message = "This account is inactive. Please contact administrator.",
                    errorCode = "ACCOUNT_INACTIVE"
                });
            }

            // ✅ STEP 5: Create authentication claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role ?? "User"),
                new Claim("UserId", user.Id.ToString()),
                new Claim("IsActive", user.IsActive.ToString()),
                new Claim("IsDeleted", user.IsDeleted.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            // ✅ STEP 6: Sign in user
            Console.WriteLine($"🔐 Signing in user: {user.Username} (Role: {user.Role})");
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            // ✅ STEP 7: Log successful login
            await _context.LogActivityAsync(user.Username, $"User {user.Username} logged in successfully");

            return Ok(new
            {
                success = true,
                message = "Login successful",
                user = user.Username,
                role = user.Role,
                isActive = user.IsActive
            });
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var username = User.Identity?.Name ?? "Unknown";

            // Log logout activity
            await _context.LogActivityAsync(username, $"User {username} logged out");

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return Ok(new
            {
                success = true,
                message = "Logout successful"
            });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            // ✅ Validate input
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Username and password are required"
                });
            }

            // ✅ Validate password strength (optional)
            if (request.Password.Length < 6)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Password must be at least 6 characters long"
                });
            }

            bool success = await _authService.RegisterAsync(request.Username, request.Password);

            if (!success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Username already exists or registration failed"
                });
            }

            return Ok(new
            {
                success = true,
                message = "User registered successfully"
            });
        }

        // ✅ Enhanced debug endpoint
        [HttpGet("debug-auth")]
        public IActionResult DebugAuth()
        {
            var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
            var username = User.Identity?.Name;
            var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            var isActive = User.Claims.FirstOrDefault(c => c.Type == "IsActive")?.Value;
            var isDeleted = User.Claims.FirstOrDefault(c => c.Type == "IsDeleted")?.Value;

            Console.WriteLine($"🔍 Debug Auth - IsAuthenticated: {isAuthenticated}");
            Console.WriteLine($"🔍 Debug Auth - Username: {username}");
            Console.WriteLine($"🔍 Debug Auth - Role: {role}");
            Console.WriteLine($"🔍 Debug Auth - IsActive: {isActive}");
            Console.WriteLine($"🔍 Debug Auth - IsDeleted: {isDeleted}");

            return Ok(new
            {
                isAuthenticated = isAuthenticated,
                username = username,
                role = role,
                isActive = isActive,
                isDeleted = isDeleted,
                claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList(),
                cookieCount = Request.Cookies.Count,
                cookies = Request.Cookies.Keys.ToList()
            });
        }

        [HttpGet("check")]
        public IActionResult CheckAuth()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var isActive = User.Claims.FirstOrDefault(c => c.Type == "IsActive")?.Value == "True";
                var isDeleted = User.Claims.FirstOrDefault(c => c.Type == "IsDeleted")?.Value == "True";

                // ✅ Additional validation from claims
                if (isDeleted)
                {
                    return Unauthorized(new
                    {
                        message = "Account has been deleted",
                        errorCode = "ACCOUNT_DELETED"
                    });
                }

                if (!isActive)
                {
                    return Unauthorized(new
                    {
                        message = "Account is inactive",
                        errorCode = "ACCOUNT_INACTIVE"
                    });
                }

                return Ok(new
                {
                    username = User.Identity.Name,
                    isActive = isActive,
                    isDeleted = isDeleted
                });
            }

            return Unauthorized(new
            {
                message = "Not authenticated",
                errorCode = "NOT_AUTHENTICATED"
            });
        }

        [HttpGet("whoami")]
        [Authorize]
        public IActionResult WhoAmI()
        {
            var username = User.Identity?.Name;
            var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            var isActive = User.Claims.FirstOrDefault(c => c.Type == "IsActive")?.Value == "True";
            var isDeleted = User.Claims.FirstOrDefault(c => c.Type == "IsDeleted")?.Value == "True";

            return Ok(new
            {
                username = username,
                role = role,
                isActive = isActive,
                isDeleted = isDeleted
            });
        }

        // ✅ NEW: Validate user status endpoint
        [HttpGet("validate-user/{username}")]
        public async Task<IActionResult> ValidateUser(string username)
        {
            if (_authService is AuthService authServiceImpl)
            {
                var validationResult = await authServiceImpl.ValidateUserAsync(username);

                return Ok(new
                {
                    isValid = validationResult.IsValid,
                    reason = validationResult.Reason,
                    statusCode = validationResult.StatusCode
                });
            }

            return BadRequest(new { message = "Validation service not available" });
        }
    }
}