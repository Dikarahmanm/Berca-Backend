using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options; // ✅ ADD THIS
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
        private readonly ILogger<AuthController> _logger; // ✅ ADD THIS

        public AuthController(IAuthService authService, AppDbContext context, ILogger<AuthController> logger) // ✅ ADD LOGGER
        {
            _authService = authService;
            _context = context;
            _logger = logger; // ✅ ADD THIS
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // ✅ Enhanced logging
            _logger.LogInformation("🔐 Login attempt for user: {Username}", request.Username);

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
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("IsActive", user.IsActive.ToString()),
                new Claim("IsDeleted", user.IsDeleted.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,  // ✅ Persist across browser sessions
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
                AllowRefresh = true
            };

            // ✅ CRITICAL: Sign in user - this sets the cookie!
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // ✅ Log successful login
            _logger.LogInformation("✅ Login successful for user: {Username}, Role: {Role}", user.Username, user.Role);

            // ✅ FIXED: Debug cookie information with proper using
            try
            {
                var cookieOptions = HttpContext.RequestServices.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>();
                var cookieName = cookieOptions.Get(CookieAuthenticationDefaults.AuthenticationScheme).Cookie.Name;
                _logger.LogInformation("🍪 Auth cookie name: {CookieName}", cookieName);

                return Ok(new
                {
                    success = true,
                    message = "Login successful",
                    user = user.Username,
                    role = user.Role,
                    isActive = user.IsActive,
                    debug = new
                    {
                        cookieSet = true,
                        cookieName = cookieName,
                        expiresAt = authProperties.ExpiresUtc
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not get cookie options: {Error}", ex.Message);

                return Ok(new
                {
                    success = true,
                    message = "Login successful",
                    user = user.Username,
                    role = user.Role,
                    isActive = user.IsActive,
                    debug = new
                    {
                        cookieSet = true,
                        cookieName = "TokoEniwanAuth", // Fallback
                        expiresAt = authProperties.ExpiresUtc
                    }
                });
            }
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

        // ✅ FIXED: Enhanced debug endpoint with proper logging
        [HttpGet("debug-auth")]
        public IActionResult DebugAuth()
        {
            var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
            var username = User.Identity?.Name;
            var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            var isActive = User.Claims.FirstOrDefault(c => c.Type == "IsActive")?.Value;
            var isDeleted = User.Claims.FirstOrDefault(c => c.Type == "IsDeleted")?.Value;

            // ✅ Use _logger instead of Console.WriteLine
            _logger.LogInformation("🔍 Debug Auth - IsAuthenticated: {IsAuthenticated}", isAuthenticated);
            _logger.LogInformation("🔍 Debug Auth - Username: {Username}", username);
            _logger.LogInformation("🔍 Debug Auth - Role: {Role}", role);
            _logger.LogInformation("🔍 Debug Auth - IsActive: {IsActive}", isActive);
            _logger.LogInformation("🔍 Debug Auth - IsDeleted: {IsDeleted}", isDeleted);

            // ✅ Enhanced cookie debugging
            var allCookies = Request.Cookies.ToDictionary(c => c.Key, c => c.Value);
            var cookieNames = Request.Cookies.Keys.ToArray();
            var authCookie = Request.Cookies["TokoEniwanAuth"];

            _logger.LogInformation("🍪 Total cookies: {Count}", allCookies.Count);
            _logger.LogInformation("🍪 Cookie names: {Names}", string.Join(", ", cookieNames));
            _logger.LogInformation("🍪 Auth cookie exists: {Exists}", !string.IsNullOrEmpty(authCookie));

            return Ok(new
            {
                success = true,
                isAuthenticated = isAuthenticated,
                user = isAuthenticated ? new
                {
                    username = username,
                    role = role,
                    isActive = isActive,
                    isDeleted = isDeleted
                } : null,
                debug = new
                {
                    timestamp = DateTime.UtcNow,
                    cookies = new
                    {
                        total = allCookies.Count,
                        names = cookieNames,
                        authCookieExists = !string.IsNullOrEmpty(authCookie),
                        authCookieLength = authCookie?.Length ?? 0
                    },
                    claims = User.Claims.Select(c => new { c.Type, c.Value }).ToArray()
                }
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