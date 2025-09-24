using Berca_Backend.DTOs;
using Berca_Backend.Services.Interfaces;
using Berca_Backend.Data;
using Berca_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace Berca_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BranchController : ControllerBase
    {
        private readonly IBranchService _branchService;
        private readonly IUserBranchAssignmentService _userBranchService;
        private readonly ITimezoneService _timezoneService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<BranchController> _logger;
        private readonly AppDbContext _context;

        public BranchController(
            IBranchService branchService,
            IUserBranchAssignmentService userBranchService,
            ITimezoneService timezoneService,
            IMemoryCache cache,
            ILogger<BranchController> logger,
            AppDbContext context)
        {
            _branchService = branchService;
            _userBranchService = userBranchService;
            _timezoneService = timezoneService;
            _cache = cache;
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// Get list of branches with filtering and pagination
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "Branch.Read")]
        public async Task<IActionResult> GetBranches([FromQuery] BranchQueryParams queryParams)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                // Apply branch access filtering based on user role
                int? accessFilter = IsAdminOrHeadManager(currentUserRole) ? null : currentUserId;

                var branches = await _branchService.GetBranchesAsync(queryParams, accessFilter);

                return Ok(ApiResponse<List<BranchDto>>.SuccessResponse(branches, 
                    $"Retrieved {branches.Count} branches"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving branches with query params: {@QueryParams}", queryParams);
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get branches accessible by current user (Required by frontend integration)
        /// </summary>
        [HttpGet("accessible")]
        [Authorize(Policy = "Branch.Read")]
        public async Task<IActionResult> GetAccessibleBranches()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                if (currentUserId == 0)
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Invalid user session"
                    });
                }

                // ✅ CACHE ASIDE PATTERN: Check cache first
                var cacheKey = $"accessible_branches_{currentUserId}_{currentUserRole}";

                if (_cache.TryGetValue(cacheKey, out object? cachedBranches))
                {
                    _logger.LogInformation("🔄 Cache HIT: Retrieved accessible branches from cache for user {UserId}", currentUserId);
                    return Ok(cachedBranches);
                }

                _logger.LogInformation("🔄 Cache MISS: Fetching accessible branches from database for user {UserId}", currentUserId);

                var branches = new List<object>();
                
                if (IsAdminOrHeadManager(currentUserRole))
                {
                    // Admin and HeadManager can access all branches
                    branches = await _context.Branches
                        .Where(b => b.IsActive)
                        .OrderBy(b => b.BranchName)
                        .Select(b => new
                        {
                            branchId = b.Id,
                            branchCode = b.BranchCode,
                            branchName = b.BranchName,
                            branchType = b.BranchType.ToString(),
                            parentBranchId = b.ParentBranchId,
                            isHeadOffice = b.BranchType == BranchType.Head,
                            level = 0, // Can be calculated based on hierarchy if needed
                            address = b.Address,
                            managerName = b.ManagerName,
                            phone = b.Phone,
                            isActive = b.IsActive,
                            canRead = true,
                            canWrite = true,
                            canApprove = true,
                            canTransfer = true,
                            createdAt = b.CreatedAt,
                            updatedAt = b.UpdatedAt
                        })
                        .Cast<object>()
                        .ToListAsync();
                }
                else
                {
                    // Get user's accessible branches via BranchAccess table
                    try
                    {
                        branches = await _context.BranchAccesses
                            .Where(ba => ba.UserId == currentUserId && ba.IsActive)
                            .Include(ba => ba.Branch)
                            .Where(ba => ba.Branch.IsActive)
                            .OrderBy(ba => ba.Branch.BranchName)
                            .Select(ba => new
                            {
                                branchId = ba.BranchId,
                                branchCode = ba.Branch.BranchCode,
                                branchName = ba.Branch.BranchName,
                                branchType = ba.Branch.BranchType.ToString(),
                                parentBranchId = ba.Branch.ParentBranchId,
                                isHeadOffice = ba.Branch.BranchType == BranchType.Head,
                                level = 0,
                                address = ba.Branch.Address,
                                managerName = ba.Branch.ManagerName,
                                phone = ba.Branch.Phone,
                                isActive = ba.Branch.IsActive,
                                canRead = ba.CanRead,
                                canWrite = ba.CanWrite,
                                canApprove = ba.CanApprove,
                                canTransfer = ba.CanTransfer,
                                createdAt = ba.Branch.CreatedAt,
                                updatedAt = ba.Branch.UpdatedAt
                            })
                            .Cast<object>()
                            .ToListAsync();
                    }
                    catch
                    {
                        // Fallback: Use user's assigned branch
                        var user = await _context.Users
                            .Include(u => u.Branch)
                            .FirstOrDefaultAsync(u => u.Id == currentUserId);
                            
                        if (user?.Branch != null)
                        {
                            branches.Add(new
                            {
                                branchId = user.Branch.Id,
                                branchCode = user.Branch.BranchCode,
                                branchName = user.Branch.BranchName,
                                branchType = user.Branch.BranchType.ToString(),
                                parentBranchId = user.Branch.ParentBranchId,
                                isHeadOffice = user.Branch.BranchType == BranchType.Head,
                                level = 0,
                                address = user.Branch.Address,
                                managerName = user.Branch.ManagerName,
                                phone = user.Branch.Phone,
                                isActive = user.Branch.IsActive,
                                canRead = true,
                                canWrite = !currentUserRole.Equals("User", StringComparison.OrdinalIgnoreCase),
                                canApprove = new[] { "Admin", "HeadManager", "BranchManager" }.Contains(currentUserRole),
                                canTransfer = new[] { "Admin", "HeadManager", "BranchManager" }.Contains(currentUserRole),
                                createdAt = user.Branch.CreatedAt,
                                updatedAt = user.Branch.UpdatedAt
                            });
                        }
                    }
                }

                // Prepare the response object
                var branchesResponse = new
                {
                    success = true,
                    data = branches
                };

                // ✅ CACHE ASIDE PATTERN: Update cache after database fetch
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60), // Branch data cache for 1 hour (changes infrequently)
                    SlidingExpiration = TimeSpan.FromMinutes(20),
                    Priority = CacheItemPriority.High // Critical for UI branch selector
                };

                _cache.Set(cacheKey, branchesResponse, cacheOptions);
                _logger.LogInformation("💾 Cache UPDATED: Stored accessible branches in cache for user {UserId}", currentUserId);

                return Ok(branchesResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving accessible branches for user {UserId}", GetCurrentUserId());
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get branch details by ID with user assignments
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Policy = "Branch.Read")]
        public async Task<IActionResult> GetBranch(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                // Check if user can access this branch
                if (!IsAdminOrHeadManager(currentUserRole))
                {
                    var hasAccess = await _userBranchService.ValidateUserBranchAccessAsync(currentUserId, id);
                    if (!hasAccess)
                    {
                        return Forbid("You do not have access to this branch");
                    }
                }

                var branchDetail = await _branchService.GetBranchDetailWithUsersAsync(id, currentUserId);

                return Ok(ApiResponse<BranchDetailDto>.SuccessResponse(branchDetail, "Branch details retrieved"));
            }
            catch (ArgumentException ex)
            {
                return NotFound(ApiResponse<object>.ErrorResponse(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving branch {BranchId}", id);
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get branch hierarchy structure grouped by region and type
        /// </summary>
        [HttpGet("hierarchy")]
        [Authorize(Policy = "Branch.Read")]
        public async Task<IActionResult> GetBranchHierarchy()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                int? accessFilter = IsAdminOrHeadManager(currentUserRole) ? null : currentUserId;

                var hierarchy = await _branchService.GetBranchHierarchyAsync(accessFilter);

                return Ok(ApiResponse<Dictionary<string, List<BranchDto>>>.SuccessResponse(hierarchy, 
                    "Branch hierarchy retrieved"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving branch hierarchy");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Create a new branch
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "Branch.Write")]
        public async Task<IActionResult> CreateBranch([FromBody] CreateBranchDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("Invalid input data",
                        ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()));
                }

                var branch = await _branchService.CreateBranchAsync(dto);

                _logger.LogInformation("Branch {BranchCode} created by user {UserId}", 
                    branch.BranchCode, GetCurrentUserId());

                return CreatedAtAction(nameof(GetBranch), new { id = branch.Id }, 
                    ApiResponse<BranchDto>.SuccessResponse(branch, "Branch created successfully"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("Validation failed", new List<string> { ex.Message }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating branch: {@BranchData}", dto);
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Update branch details
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Policy = "Branch.Write")]
        public async Task<IActionResult> UpdateBranch(int id, [FromBody] UpdateBranchDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("Invalid input data",
                        ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()));
                }

                var branch = await _branchService.UpdateBranchAsync(id, dto);

                if (branch == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Branch not found"));
                }

                _logger.LogInformation("Branch {BranchId} updated by user {UserId}", id, GetCurrentUserId());

                return Ok(ApiResponse<BranchDto>.SuccessResponse(branch, "Branch updated successfully"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("Validation failed", new List<string> { ex.Message }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating branch {BranchId}: {@UpdateData}", id, dto);
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Soft delete (deactivate) a branch
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = "Branch.Write")]
        public async Task<IActionResult> SoftDeleteBranch(int id)
        {
            try
            {
                var result = await _branchService.SoftDeleteBranchAsync(id);

                if (!result)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Branch not found"));
                }

                _logger.LogInformation("Branch {BranchId} soft deleted by user {UserId}", id, GetCurrentUserId());

                return Ok(ApiResponse<object>.SuccessResponse(new { }, "Branch deactivated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft deleting branch {BranchId}", id);
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Permanently delete a branch (Admin only)
        /// </summary>
        [HttpDelete("{id}/permanent")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> DeleteBranchPermanently(int id)
        {
            try
            {
                var result = await _branchService.DeleteBranchAsync(id);

                if (!result)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Branch not found"));
                }

                _logger.LogInformation("Branch {BranchId} permanently deleted by user {UserId}", id, GetCurrentUserId());

                return Ok(ApiResponse<object>.SuccessResponse(new { }, "Branch deleted permanently"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error permanently deleting branch {BranchId}", id);
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get branch performance comparison metrics
        /// </summary>
        [HttpGet("performance")]
        [Authorize(Policy = "Reports.Branch")]
        public async Task<IActionResult> GetBranchPerformance([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                int? accessFilter = IsAdminOrHeadManager(currentUserRole) ? null : currentUserId;

                var performance = await _branchService.GetBranchPerformanceAsync(startDate, endDate, accessFilter);

                return Ok(ApiResponse<List<BranchPerformanceDto>>.SuccessResponse(performance, 
                    $"Branch performance data retrieved for {performance.Count} branches"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving branch performance data");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get branch performance comparison with consolidated totals
        /// </summary>
        [HttpGet("performance/comparison")]
        [Authorize(Policy = "Reports.Consolidated")]
        public async Task<IActionResult> GetBranchComparison([FromQuery] DateTime? compareDate)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                int? accessFilter = IsAdminOrHeadManager(currentUserRole) ? null : currentUserId;

                var comparison = await _branchService.GetBranchComparisonAsync(compareDate, accessFilter);

                return Ok(ApiResponse<BranchComparisonDto>.SuccessResponse(comparison, 
                    "Branch comparison data retrieved"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving branch comparison data");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get branches grouped by region (province/city)
        /// </summary>
        [HttpGet("by-region")]
        [Authorize(Policy = "Branch.Read")]
        public async Task<IActionResult> GetBranchesByRegion([FromQuery] string? province, [FromQuery] string? city)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                int? accessFilter = IsAdminOrHeadManager(currentUserRole) ? null : currentUserId;

                var branches = await _branchService.GetBranchesByRegionAsync(province, city, accessFilter);

                var groupedByProvince = branches
                    .GroupBy(b => b.Province)
                    .ToDictionary(g => g.Key, g => g.GroupBy(b => b.City).ToDictionary(c => c.Key, c => c.ToList()));

                return Ok(ApiResponse<Dictionary<string, Dictionary<string, List<BranchDto>>>>.SuccessResponse(
                    groupedByProvince, $"Retrieved branches by region - {branches.Count} total branches"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving branches by region");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get store size analytics
        /// </summary>
        [HttpGet("analytics/store-size")]
        [Authorize(Policy = "Reports.Branch")]
        public async Task<IActionResult> GetStoreSizeAnalytics()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                int? accessFilter = IsAdminOrHeadManager(currentUserRole) ? null : currentUserId;

                var storeSizeData = await _branchService.GetBranchCountByStoreSizeAsync(accessFilter);

                return Ok(ApiResponse<Dictionary<string, int>>.SuccessResponse(storeSizeData, 
                    "Store size analytics retrieved"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving store size analytics");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get regional distribution analytics
        /// </summary>
        [HttpGet("analytics/regional")]
        [Authorize(Policy = "Reports.Branch")]
        public async Task<IActionResult> GetRegionalAnalytics()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                int? accessFilter = IsAdminOrHeadManager(currentUserRole) ? null : currentUserId;

                var regionalData = await _branchService.GetBranchCountByRegionAsync(accessFilter);

                return Ok(ApiResponse<Dictionary<string, int>>.SuccessResponse(regionalData, 
                    "Regional distribution analytics retrieved"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving regional analytics");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get user count summaries for all branches
        /// </summary>
        [HttpGet("user-summaries")]
        [Authorize(Policy = "Branch.Manage")]
        public async Task<IActionResult> GetBranchUserSummaries()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                int? accessFilter = IsAdminOrHeadManager(currentUserRole) ? null : currentUserId;

                var summaries = await _branchService.GetBranchUserSummariesAsync(accessFilter);

                return Ok(ApiResponse<List<BranchUserSummaryDto>>.SuccessResponse(summaries, 
                    $"User summaries retrieved for {summaries.Count} branches"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving branch user summaries");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get only active branches
        /// </summary>
        [HttpGet("active")]
        [Authorize(Policy = "Branch.Read")]
        public async Task<IActionResult> GetActiveBranches()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                int? accessFilter = IsAdminOrHeadManager(currentUserRole) ? null : currentUserId;

                var branches = await _branchService.GetActiveBranchesAsync(accessFilter);

                return Ok(ApiResponse<List<BranchDto>>.SuccessResponse(branches, 
                    $"Retrieved {branches.Count} active branches"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active branches");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get only inactive branches (Admin/HeadManager only)
        /// </summary>
        [HttpGet("inactive")]
        [Authorize(Policy = "MultiBranch.Access")]
        public async Task<IActionResult> GetInactiveBranches()
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                var branches = await _branchService.GetInactiveBranchesAsync(currentUserId);

                return Ok(ApiResponse<List<BranchDto>>.SuccessResponse(branches, 
                    $"Retrieved {branches.Count} inactive branches"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving inactive branches");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Toggle branch active status
        /// </summary>
        [HttpPatch("{id}/toggle-status")]
        [Authorize(Policy = "Branch.Write")]
        public async Task<IActionResult> ToggleBranchStatus(int id, [FromBody] BranchStatusChangeDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("Invalid input data",
                        ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()));
                }

                if (dto.BranchId != id)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("Branch ID mismatch"));
                }

                var result = await _branchService.ToggleBranchStatusAsync(id, dto.IsActive);

                if (!result)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Branch not found"));
                }

                _logger.LogInformation("Branch {BranchId} status changed to {Status} by user {UserId}. Reason: {Reason}", 
                    id, dto.IsActive ? "Active" : "Inactive", GetCurrentUserId(), dto.Reason ?? "Not specified");

                return Ok(ApiResponse<object>.SuccessResponse(new { }, 
                    $"Branch status changed to {(dto.IsActive ? "Active" : "Inactive")}"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling branch status for branch {BranchId}", id);
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Validate branch code uniqueness
        /// </summary>
        [HttpGet("validate-code/{branchCode}")]
        [Authorize(Policy = "Branch.Write")]
        public async Task<IActionResult> ValidateBranchCode(string branchCode, [FromQuery] int? excludeBranchId)
        {
            try
            {
                var isUnique = await _branchService.IsBranchCodeUniqueAsync(branchCode, excludeBranchId);

                return Ok(ApiResponse<bool>.SuccessResponse(isUnique, 
                    isUnique ? "Branch code is available" : "Branch code is already in use"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating branch code {BranchCode}", branchCode);
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get current user's accessible branches
        /// </summary>
        [HttpGet("my-accessible")]
        [Authorize]
        public async Task<IActionResult> GetMyAccessibleBranches()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var accessibleBranches = await _userBranchService.GetAccessibleBranchesForUserAsync(currentUserId);

                return Ok(ApiResponse<List<BranchAccessDto>>.SuccessResponse(accessibleBranches, 
                    $"Retrieved {accessibleBranches.Count} accessible branches"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving accessible branches for user {UserId}", GetCurrentUserId());
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get comprehensive branch analytics
        /// </summary>
        [HttpGet("analytics/comprehensive")]
        [Authorize(Policy = "Reports.Consolidated")]
        public async Task<IActionResult> GetComprehensiveAnalytics()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                int? accessFilter = IsAdminOrHeadManager(currentUserRole) ? null : currentUserId;

                var storeSizeData = await _branchService.GetBranchCountByStoreSizeAsync(accessFilter);
                var regionalData = await _branchService.GetBranchCountByRegionAsync(accessFilter);
                var userSummaries = await _branchService.GetBranchUserSummariesAsync(accessFilter);

                var analytics = new BranchAnalyticsDto
                {
                    BranchCountByStoreSize = storeSizeData,
                    BranchCountByRegion = regionalData,
                    BranchCountByType = userSummaries
                        .GroupBy(s => s.BranchType.ToString())
                        .ToDictionary(g => g.Key, g => g.Count()),
                    UserCountByBranch = userSummaries
                        .ToDictionary(s => s.BranchName, s => s.TotalUsers),
                    TotalBranches = userSummaries.Count,
                    ActiveBranches = userSummaries.Count(s => s.IsActive),
                    InactiveBranches = userSummaries.Count(s => !s.IsActive),
                    TotalEmployees = userSummaries.Sum(s => s.TotalUsers),
                    AverageEmployeesPerBranch = userSummaries.Count > 0 
                        ? (decimal)userSummaries.Sum(s => s.TotalUsers) / userSummaries.Count 
                        : 0,
                    GeneratedAt = _timezoneService.Now
                };

                return Ok(ApiResponse<BranchAnalyticsDto>.SuccessResponse(analytics, 
                    "Comprehensive branch analytics retrieved"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving comprehensive analytics");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        #region Helper Methods

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value ?? 
                             User.FindFirst("sub")?.Value ?? 
                             User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        private string GetCurrentUserRole()
        {
            return User.FindFirst("Role")?.Value ?? 
                   User.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value ?? 
                   User.FindFirst(ClaimTypes.Role)?.Value ??
                   "User";
        }

        private bool IsAdminOrHeadManager(string role)
        {
            return role.ToUpper() is "ADMIN" or "HEADMANAGER";
        }

        #endregion
    }
}