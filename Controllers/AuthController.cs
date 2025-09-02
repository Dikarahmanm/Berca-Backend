using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options; // ✅ ADD THIS
using Microsoft.EntityFrameworkCore;
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

            // ✅ Log successful login
            await _context.LogActivityAsync(user.Username, $"User {user.Username} logged in successfully");

            return Ok(new
            {
                success = true,
                data = new
                {
                    user = new
                    {
                        id = user.Id,
                        username = user.Username,
                        email = user.UserProfile?.Email ?? $"{user.Username}@tokoeniwan.com", 
                        role = user.Role,
                        fullName = user.UserProfile?.FullName ?? user.Username,
                        defaultBranchId = user.BranchId,
                        accessibleBranches = user.GetAccessibleBranchIds()
                    },
                    sessionExpiresAt = authProperties.ExpiresUtc
                }
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

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var username = User.Identity?.Name;
            var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            var isActive = User.Claims.FirstOrDefault(c => c.Type == "IsActive")?.Value == "True";
            var isDeleted = User.Claims.FirstOrDefault(c => c.Type == "IsDeleted")?.Value == "True";
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(username) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid session",
                    errorCode = "INVALID_SESSION"
                });
            }

            try
            {
                // Get user with profile information
                var user = await _context.Users
                    .Include(u => u.UserProfile)
                    .Include(u => u.Branch)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "User not found",
                        errorCode = "USER_NOT_FOUND"
                    });
                }

                // Get branch access permissions (if BranchAccess table exists and is populated)
                var branchAccess = new List<object>();
                try
                {
                    var branchAccessList = await _context.BranchAccesses
                        .Where(ba => ba.UserId == userId && ba.IsActive)
                        .Include(ba => ba.Branch)
                        .Select(ba => new
                        {
                            branchId = ba.BranchId,
                            canRead = ba.CanRead,
                            canWrite = ba.CanWrite,
                            canApprove = ba.CanApprove,
                            canTransfer = ba.CanTransfer,
                            assignedAt = ba.AssignedAt
                        })
                        .ToListAsync();
                    branchAccess = branchAccessList.Cast<object>().ToList();
                }
                catch
                {
                    // BranchAccess table might not exist yet, use fallback
                    if (user.BranchId.HasValue)
                    {
                        branchAccess.Add(new
                        {
                            branchId = user.BranchId.Value,
                            canRead = true,
                            canWrite = user.Role != "User",
                            canApprove = new[] { "Admin", "HeadManager", "BranchManager" }.Contains(user.Role),
                            canTransfer = new[] { "Admin", "HeadManager", "BranchManager" }.Contains(user.Role),
                            assignedAt = user.CreatedAt
                        });
                    }
                }

                // Get current branch from cookie (if set)
                var currentBranchId = HttpContext.Request.Cookies[".TokoEniwan.BranchContext"];
                int? parsedCurrentBranchId = null;
                if (int.TryParse(currentBranchId, out var branchId))
                {
                    parsedCurrentBranchId = branchId;
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        userId = user.Id,
                        username = user.Username,
                        role = user.Role,
                        fullName = user.UserProfile?.FullName ?? user.Username,
                        currentBranchId = parsedCurrentBranchId ?? user.BranchId,
                        defaultBranchId = user.BranchId,
                        branchAccess = branchAccess,
                        sessionExpiresAt = DateTimeOffset.UtcNow.Add(TimeSpan.FromHours(8)) // Match cookie expiration
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current user information for user {UserId}", userId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error"
                });
            }
        }

        [HttpPost("switch-branch")]
        [Authorize]
        public async Task<IActionResult> SwitchBranch([FromBody] SwitchBranchRequest request)
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var username = User.Identity?.Name;

            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid session"
                });
            }

            try
            {
                // Get user and validate branch access
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "User not found"
                    });
                }

                // Validate branch access
                bool hasAccess = false;
                
                // Admin can access any branch
                if (user.Role == "Admin")
                {
                    hasAccess = true;
                }
                else
                {
                    // Check if user has access to this branch
                    try
                    {
                        hasAccess = await _context.BranchAccesses
                            .AnyAsync(ba => ba.UserId == userId && ba.BranchId == request.BranchId && ba.IsActive && ba.CanRead);
                    }
                    catch
                    {
                        // Fallback: check user's assigned branch or accessible branches
                        hasAccess = user.CanAccessBranch(request.BranchId);
                    }
                }

                if (!hasAccess)
                {
                    return StatusCode(403, new
                    {
                        success = false,
                        message = "Access denied to this branch"
                    });
                }

                // Set branch context cookie
                Response.Cookies.Append(".TokoEniwan.BranchContext", request.BranchId.ToString(), new CookieOptions
                {
                    HttpOnly = true,
                    Secure = HttpContext.Request.IsHttps,
                    SameSite = SameSiteMode.Strict,
                    Path = "/",
                    MaxAge = TimeSpan.FromHours(8)
                });

                // Log branch switch
                await _context.LogActivityAsync(username ?? "Unknown", $"User switched to branch {request.BranchId}");

                return Ok(new
                {
                    success = true,
                    message = "Branch context switched successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error switching branch for user {UserId} to branch {BranchId}", userId, request.BranchId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error"
                });
            }
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