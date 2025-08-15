using Berca_Backend.DTOs;
using Berca_Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Berca_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserBranchAssignmentController : ControllerBase
    {
        private readonly IUserBranchAssignmentService _assignmentService;
        private readonly ILogger<UserBranchAssignmentController> _logger;

        public UserBranchAssignmentController(
            IUserBranchAssignmentService assignmentService,
            ILogger<UserBranchAssignmentController> logger)
        {
            _assignmentService = assignmentService;
            _logger = logger;
        }

        /// <summary>
        /// Assign a user to a specific branch with access permissions
        /// </summary>
        [HttpPost("assign")]
        [Authorize(Roles = "Admin,HeadManager")]
        public async Task<IActionResult> AssignUserToBranch([FromBody] AssignUserToBranchDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("Invalid input data", 
                        ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()));
                }

                var result = await _assignmentService.AssignUserToBranchAsync(dto);
                
                if (result.Success)
                {
                    return Ok(ApiResponse<UserAssignmentStatusDto>.SuccessResponse(
                        result.UserAssignment!, result.Message));
                }
                
                return BadRequest(ApiResponse<object>.ErrorResponse(result.Message, result.Errors));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AssignUserToBranch endpoint");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Bulk assign multiple users to a branch
        /// </summary>
        [HttpPost("bulk-assign")]
        [Authorize(Roles = "Admin,HeadManager")]
        public async Task<IActionResult> BulkAssignUsersToBranch([FromBody] BulkAssignUsersToBranchDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("Invalid input data",
                        ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()));
                }

                if (!dto.UserIds.Any())
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("At least one user ID must be provided"));
                }

                var result = await _assignmentService.BulkAssignUsersToBranchAsync(dto);
                
                return Ok(ApiResponse<BulkAssignmentResultDto>.SuccessResponse(result, 
                    $"Bulk assignment completed. {result.SuccessCount} successful, {result.FailureCount} failed."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in BulkAssignUsersToBranch endpoint");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Update branch access permissions for a user
        /// </summary>
        [HttpPut("update-access")]
        [Authorize(Roles = "Admin,HeadManager")]
        public async Task<IActionResult> UpdateBranchAccess([FromBody] UpdateBranchAccessDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("Invalid input data",
                        ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()));
                }

                var result = await _assignmentService.UpdateBranchAccessAsync(dto);
                
                if (result.Success)
                {
                    return Ok(ApiResponse<UserAssignmentStatusDto>.SuccessResponse(
                        result.UserAssignment!, result.Message));
                }
                
                return BadRequest(ApiResponse<object>.ErrorResponse(result.Message, result.Errors));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateBranchAccess endpoint");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get assignment status for a specific user
        /// </summary>
        [HttpGet("user/{userId}/status")]
        [Authorize(Roles = "Admin,HeadManager,BranchManager")]
        public async Task<IActionResult> GetUserAssignmentStatus(int userId)
        {
            try
            {
                // Authorization check: Users can only view their own status unless they're managers/admin
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();
                
                if (currentUserId != userId && !IsManagerOrAdmin(currentUserRole))
                {
                    return Forbid("You can only view your own assignment status");
                }

                var status = await _assignmentService.GetUserAssignmentStatusAsync(userId);
                
                if (status == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("User not found"));
                }
                
                return Ok(ApiResponse<UserAssignmentStatusDto>.SuccessResponse(status, "User assignment status retrieved"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUserAssignmentStatus endpoint for user {UserId}", userId);
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get all users assigned to a specific branch
        /// </summary>
        [HttpGet("branch/{branchId}/users")]
        [Authorize(Roles = "Admin,HeadManager,BranchManager")]
        public async Task<IActionResult> GetUsersByBranch(int branchId)
        {
            try
            {
                // Authorization check: Branch managers can only view their own branch
                var currentUserRole = GetCurrentUserRole();
                if (currentUserRole == "BranchManager")
                {
                    var currentUserId = GetCurrentUserId();
                    var userStatus = await _assignmentService.GetUserAssignmentStatusAsync(currentUserId);
                    
                    if (userStatus?.BranchId != branchId && 
                        !userStatus?.AccessibleBranches.Any(b => b.BranchId == branchId) == true)
                    {
                        return Forbid("You can only view users from your assigned branch");
                    }
                }

                var branchUsers = await _assignmentService.GetUsersByBranchAsync(branchId);
                
                return Ok(ApiResponse<BranchUserListDto>.SuccessResponse(branchUsers, "Branch users retrieved"));
            }
            catch (ArgumentException ex)
            {
                return NotFound(ApiResponse<object>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUsersByBranch endpoint for branch {BranchId}", branchId);
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Query users with branch assignment filters
        /// </summary>
        [HttpGet("users")]
        [Authorize(Roles = "Admin,HeadManager")]
        public async Task<IActionResult> GetUsersWithQuery([FromQuery] UserBranchQueryParams queryParams)
        {
            try
            {
                var users = await _assignmentService.GetUsersWithQueryAsync(queryParams);
                
                return Ok(ApiResponse<List<UserAssignmentStatusDto>>.SuccessResponse(users, 
                    $"Retrieved {users.Count} users matching query"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUsersWithQuery endpoint");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Unassign a user from their current branch
        /// </summary>
        [HttpDelete("unassign/{userId}")]
        [Authorize(Roles = "Admin,HeadManager")]
        public async Task<IActionResult> UnassignUserFromBranch(int userId)
        {
            try
            {
                var result = await _assignmentService.UnassignUserFromBranchAsync(userId);
                
                if (result.Success)
                {
                    return Ok(ApiResponse<object>.SuccessResponse(new { }, result.Message));
                }
                
                return BadRequest(ApiResponse<object>.ErrorResponse(result.Message, result.Errors));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UnassignUserFromBranch endpoint for user {UserId}", userId);
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get all users without branch assignment
        /// </summary>
        [HttpGet("unassigned")]
        [Authorize(Roles = "Admin,HeadManager")]
        public async Task<IActionResult> GetUnassignedUsers()
        {
            try
            {
                var users = await _assignmentService.GetUnassignedUsersAsync();
                
                return Ok(ApiResponse<List<UserAssignmentStatusDto>>.SuccessResponse(users, 
                    $"Retrieved {users.Count} unassigned users"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUnassignedUsers endpoint");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Validate if a user has access to a specific branch
        /// </summary>
        [HttpGet("validate-access/{userId}/{branchId}")]
        [Authorize(Roles = "Admin,HeadManager,BranchManager")]
        public async Task<IActionResult> ValidateUserBranchAccess(int userId, int branchId)
        {
            try
            {
                var hasAccess = await _assignmentService.ValidateUserBranchAccessAsync(userId, branchId);
                
                return Ok(ApiResponse<bool>.SuccessResponse(hasAccess, 
                    hasAccess ? "User has access to the branch" : "User does not have access to the branch"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ValidateUserBranchAccess endpoint for user {UserId} and branch {BranchId}", 
                    userId, branchId);
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get all branches accessible by a user
        /// </summary>
        [HttpGet("user/{userId}/accessible-branches")]
        [Authorize]
        public async Task<IActionResult> GetAccessibleBranchesForUser(int userId)
        {
            try
            {
                // Authorization check: Users can only view their own accessible branches unless they're managers/admin
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();
                
                if (currentUserId != userId && !IsManagerOrAdmin(currentUserRole))
                {
                    return Forbid("You can only view your own accessible branches");
                }

                var branches = await _assignmentService.GetAccessibleBranchesForUserAsync(userId);
                
                return Ok(ApiResponse<List<BranchAccessDto>>.SuccessResponse(branches, 
                    $"Retrieved {branches.Count} accessible branches for user"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAccessibleBranchesForUser endpoint for user {UserId}", userId);
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Auto-assign user to branch based on their role
        /// </summary>
        [HttpPost("auto-assign/{userId}")]
        [Authorize(Roles = "Admin,HeadManager")]
        public async Task<IActionResult> AutoAssignUserBasedOnRole(int userId)
        {
            try
            {
                var result = await _assignmentService.AutoAssignUserBasedOnRoleAsync(userId);
                
                if (result.Success)
                {
                    return Ok(ApiResponse<UserAssignmentStatusDto>.SuccessResponse(
                        result.UserAssignment!, result.Message));
                }
                
                return BadRequest(ApiResponse<object>.ErrorResponse(result.Message, result.Errors));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AutoAssignUserBasedOnRole endpoint for user {UserId}", userId);
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Fix all users with NULL branch assignments
        /// </summary>
        [HttpPost("fix-null-assignments")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> FixNullBranchAssignments()
        {
            try
            {
                var fixedCount = await _assignmentService.FixNullBranchAssignmentsAsync();
                
                return Ok(ApiResponse<int>.SuccessResponse(fixedCount, 
                    $"Fixed {fixedCount} users with NULL branch assignments"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in FixNullBranchAssignments endpoint");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get current user's assignment status
        /// </summary>
        [HttpGet("my-status")]
        [Authorize]
        public async Task<IActionResult> GetMyAssignmentStatus()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var status = await _assignmentService.GetUserAssignmentStatusAsync(currentUserId);
                
                if (status == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("User assignment not found"));
                }
                
                return Ok(ApiResponse<UserAssignmentStatusDto>.SuccessResponse(status, "Your assignment status retrieved"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetMyAssignmentStatus endpoint");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get current user's accessible branches
        /// </summary>
        [HttpGet("my-accessible-branches")]
        [Authorize]
        public async Task<IActionResult> GetMyAccessibleBranches()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var branches = await _assignmentService.GetAccessibleBranchesForUserAsync(currentUserId);
                
                return Ok(ApiResponse<List<BranchAccessDto>>.SuccessResponse(branches, 
                    $"Retrieved {branches.Count} accessible branches"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetMyAccessibleBranches endpoint");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        #region Helper Methods

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst("sub")?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        private string GetCurrentUserRole()
        {
            return User.FindFirst("Role")?.Value ?? 
                   User.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value ?? 
                   "User";
        }

        private bool IsManagerOrAdmin(string role)
        {
            return role.ToUpper() is "ADMIN" or "HEADMANAGER" or "BRANCHMANAGER";
        }

        #endregion
    }
}