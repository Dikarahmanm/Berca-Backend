using Berca_Backend.Data;
using Berca_Backend.DTOs;
using Berca_Backend.Models;
using Berca_Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Berca_Backend.Services
{
    public class UserBranchAssignmentService : IUserBranchAssignmentService
    {
        private readonly AppDbContext _context;
        private readonly ITimezoneService _timezoneService;
        private readonly ILogger<UserBranchAssignmentService> _logger;

        public UserBranchAssignmentService(
            AppDbContext context,
            ITimezoneService timezoneService,
            ILogger<UserBranchAssignmentService> logger)
        {
            _context = context;
            _timezoneService = timezoneService;
            _logger = logger;
        }

        public async Task<AssignmentResultDto> AssignUserToBranchAsync(AssignUserToBranchDto dto)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.UserProfile)
                    .FirstOrDefaultAsync(u => u.Id == dto.UserId);

                if (user == null)
                {
                    return new AssignmentResultDto
                    {
                        Success = false,
                        Message = "User not found",
                        Errors = new List<string> { $"User with ID {dto.UserId} does not exist" },
                        ProcessedAt = _timezoneService.Now
                    };
                }

                // Validate branch exists if assignment is not null
                if (dto.BranchId.HasValue)
                {
                    var branch = await _context.Branches.FindAsync(dto.BranchId.Value);
                    if (branch == null)
                    {
                        return new AssignmentResultDto
                        {
                            Success = false,
                            Message = "Branch not found",
                            Errors = new List<string> { $"Branch with ID {dto.BranchId} does not exist" },
                            ProcessedAt = _timezoneService.Now
                        };
                    }

                    if (!branch.IsActive)
                    {
                        return new AssignmentResultDto
                        {
                            Success = false,
                            Message = "Cannot assign to inactive branch",
                            Errors = new List<string> { "The specified branch is not active" },
                            ProcessedAt = _timezoneService.Now
                        };
                    }
                }

                // Validate accessible branches exist
                var validationResult = await ValidateAccessibleBranchesAsync(dto.AccessibleBranchIds);
                if (!validationResult.Success)
                {
                    return validationResult;
                }

                // Apply role-based assignment logic
                var roleValidationResult = await ValidateRoleBasedAssignmentAsync(user, dto);
                if (!roleValidationResult.Success)
                {
                    return roleValidationResult;
                }

                // Update user assignment
                user.BranchId = dto.BranchId;
                user.CanAccessMultipleBranches = dto.CanAccessMultipleBranches;
                user.SetAccessibleBranchIds(dto.AccessibleBranchIds);
                user.UpdatedAt = _timezoneService.Now;

                await _context.SaveChangesAsync();

                var userAssignment = await GetUserAssignmentStatusAsync(user.Id);

                _logger.LogInformation("User {UserId} assigned to branch {BranchId} by system", 
                    user.Id, dto.BranchId);

                return new AssignmentResultDto
                {
                    Success = true,
                    Message = "User successfully assigned to branch",
                    UserAssignment = userAssignment,
                    ProcessedAt = _timezoneService.Now
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning user {UserId} to branch {BranchId}", 
                    dto.UserId, dto.BranchId);

                return new AssignmentResultDto
                {
                    Success = false,
                    Message = "An error occurred while assigning user",
                    Errors = new List<string> { ex.Message },
                    ProcessedAt = _timezoneService.Now
                };
            }
        }

        public async Task<BulkAssignmentResultDto> BulkAssignUsersToBranchAsync(BulkAssignUsersToBranchDto dto)
        {
            var results = new List<AssignmentResultDto>();
            var successCount = 0;

            foreach (var userId in dto.UserIds)
            {
                var assignDto = new AssignUserToBranchDto
                {
                    UserId = userId,
                    BranchId = dto.BranchId,
                    CanAccessMultipleBranches = dto.CanAccessMultipleBranches,
                    AccessibleBranchIds = dto.AccessibleBranchIds
                };

                var result = await AssignUserToBranchAsync(assignDto);
                results.Add(result);

                if (result.Success)
                    successCount++;
            }

            return new BulkAssignmentResultDto
            {
                OverallSuccess = successCount == dto.UserIds.Count,
                SuccessCount = successCount,
                FailureCount = dto.UserIds.Count - successCount,
                Results = results,
                ProcessedAt = _timezoneService.Now
            };
        }

        public async Task<AssignmentResultDto> UpdateBranchAccessAsync(UpdateBranchAccessDto dto)
        {
            try
            {
                var user = await _context.Users.FindAsync(dto.UserId);

                if (user == null)
                {
                    return new AssignmentResultDto
                    {
                        Success = false,
                        Message = "User not found",
                        ProcessedAt = _timezoneService.Now
                    };
                }

                // Validate accessible branches
                var validationResult = await ValidateAccessibleBranchesAsync(dto.AccessibleBranchIds);
                if (!validationResult.Success)
                {
                    return validationResult;
                }

                user.CanAccessMultipleBranches = dto.CanAccessMultipleBranches;
                user.SetAccessibleBranchIds(dto.AccessibleBranchIds);
                user.UpdatedAt = _timezoneService.Now;

                await _context.SaveChangesAsync();

                var userAssignment = await GetUserAssignmentStatusAsync(user.Id);

                return new AssignmentResultDto
                {
                    Success = true,
                    Message = "Branch access updated successfully",
                    UserAssignment = userAssignment,
                    ProcessedAt = _timezoneService.Now
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating branch access for user {UserId}", dto.UserId);

                return new AssignmentResultDto
                {
                    Success = false,
                    Message = "An error occurred while updating branch access",
                    Errors = new List<string> { ex.Message },
                    ProcessedAt = _timezoneService.Now
                };
            }
        }

        public async Task<UserAssignmentStatusDto?> GetUserAssignmentStatusAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.UserProfile)
                .Include(u => u.Branch)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return null;

            var accessibleBranches = await GetAccessibleBranchesForUserAsync(userId);

            return new UserAssignmentStatusDto
            {
                UserId = user.Id,
                Username = user.Username,
                FullName = user.UserProfile?.FullName ?? "",
                Role = user.Role,
                BranchId = user.BranchId,
                BranchName = user.Branch?.BranchName,
                BranchCity = user.Branch?.City,
                CanAccessMultipleBranches = user.CanAccessMultipleBranches,
                AccessibleBranches = accessibleBranches,
                IsActive = user.IsActive,
                AssignedAt = user.CreatedAt,
                LastUpdated = user.UpdatedAt
            };
        }

        public async Task<BranchUserListDto> GetUsersByBranchAsync(int branchId)
        {
            var branch = await _context.Branches
                .Include(b => b.Users)
                    .ThenInclude(u => u.UserProfile)
                .FirstOrDefaultAsync(b => b.Id == branchId);

            if (branch == null)
                throw new ArgumentException("Branch not found");

            // Get users assigned to this branch
            var assignedUsers = await _context.Users
                .Include(u => u.UserProfile)
                .Include(u => u.Branch)
                .Where(u => u.BranchId == branchId)
                .ToListAsync();

            // Get users with access to this branch via AccessibleBranchIds
            var usersWithAccess = await _context.Users
                .Include(u => u.UserProfile)
                .Include(u => u.Branch)
                .Where(u => u.CanAccessMultipleBranches && 
                           u.AccessibleBranchIds != null && 
                           u.AccessibleBranchIds.Contains(branchId.ToString()))
                .ToListAsync();

            var assignedUserDtos = await Task.WhenAll(
                assignedUsers.Select(async u => await GetUserAssignmentStatusAsync(u.Id))
            );

            var accessUserDtos = await Task.WhenAll(
                usersWithAccess.Select(async u => await GetUserAssignmentStatusAsync(u.Id))
            );

            return new BranchUserListDto
            {
                BranchId = branch.Id,
                BranchName = branch.BranchName,
                BranchCode = branch.BranchCode,
                City = branch.City,
                AssignedUsers = assignedUserDtos.Where(u => u != null).ToList()!,
                UsersWithAccess = accessUserDtos.Where(u => u != null).ToList()!,
                TotalUserCount = assignedUsers.Count + usersWithAccess.Count,
                ActiveUserCount = assignedUsers.Count(u => u.IsActive) + usersWithAccess.Count(u => u.IsActive)
            };
        }

        public async Task<List<UserAssignmentStatusDto>> GetUsersWithQueryAsync(UserBranchQueryParams queryParams)
        {
            var query = _context.Users
                .Include(u => u.UserProfile)
                .Include(u => u.Branch)
                .AsQueryable();

            // Apply filters
            if (queryParams.BranchId.HasValue)
            {
                query = query.Where(u => u.BranchId == queryParams.BranchId);
            }

            if (!string.IsNullOrEmpty(queryParams.Role))
            {
                query = query.Where(u => u.Role == queryParams.Role);
            }

            if (queryParams.CanAccessMultipleBranches.HasValue)
            {
                query = query.Where(u => u.CanAccessMultipleBranches == queryParams.CanAccessMultipleBranches);
            }

            if (queryParams.IsActive.HasValue)
            {
                query = query.Where(u => u.IsActive == queryParams.IsActive);
            }

            if (queryParams.HasBranchAssignment.HasValue)
            {
                if (queryParams.HasBranchAssignment.Value)
                    query = query.Where(u => u.BranchId != null);
                else
                    query = query.Where(u => u.BranchId == null);
            }

            if (!string.IsNullOrEmpty(queryParams.Search))
            {
                var search = queryParams.Search.ToLower();
                query = query.Where(u => u.Username.ToLower().Contains(search) ||
                                       (u.UserProfile != null && u.UserProfile.FullName.ToLower().Contains(search)));
            }

            // Apply sorting
            switch (queryParams.SortBy?.ToLower())
            {
                case "username":
                    query = queryParams.SortOrder?.ToLower() == "desc" 
                        ? query.OrderByDescending(u => u.Username)
                        : query.OrderBy(u => u.Username);
                    break;
                case "role":
                    query = queryParams.SortOrder?.ToLower() == "desc"
                        ? query.OrderByDescending(u => u.Role)
                        : query.OrderBy(u => u.Role);
                    break;
                case "branchname":
                    query = queryParams.SortOrder?.ToLower() == "desc"
                        ? query.OrderByDescending(u => u.Branch!.BranchName)
                        : query.OrderBy(u => u.Branch!.BranchName);
                    break;
                default:
                    query = query.OrderBy(u => u.Username);
                    break;
            }

            // Apply pagination
            var skip = (queryParams.Page - 1) * queryParams.PageSize;
            var users = await query.Skip(skip).Take(queryParams.PageSize).ToListAsync();

            var result = new List<UserAssignmentStatusDto>();
            foreach (var user in users)
            {
                var status = await GetUserAssignmentStatusAsync(user.Id);
                if (status != null)
                    result.Add(status);
            }

            return result;
        }

        public async Task<AssignmentResultDto> UnassignUserFromBranchAsync(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    return new AssignmentResultDto
                    {
                        Success = false,
                        Message = "User not found",
                        ProcessedAt = _timezoneService.Now
                    };
                }

                user.BranchId = null;
                user.CanAccessMultipleBranches = false;
                user.AccessibleBranchIds = null;
                user.UpdatedAt = _timezoneService.Now;

                await _context.SaveChangesAsync();

                return new AssignmentResultDto
                {
                    Success = true,
                    Message = "User successfully unassigned from branch",
                    ProcessedAt = _timezoneService.Now
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unassigning user {UserId} from branch", userId);

                return new AssignmentResultDto
                {
                    Success = false,
                    Message = "An error occurred while unassigning user",
                    Errors = new List<string> { ex.Message },
                    ProcessedAt = _timezoneService.Now
                };
            }
        }

        public async Task<List<UserAssignmentStatusDto>> GetUnassignedUsersAsync()
        {
            var users = await _context.Users
                .Include(u => u.UserProfile)
                .Where(u => u.BranchId == null && u.IsActive)
                .ToListAsync();

            var result = new List<UserAssignmentStatusDto>();
            foreach (var user in users)
            {
                var status = await GetUserAssignmentStatusAsync(user.Id);
                if (status != null)
                    result.Add(status);
            }

            return result;
        }

        public async Task<bool> ValidateUserBranchAccessAsync(int userId, int branchId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || !user.IsActive)
                return false;

            // Admin has access to all branches
            if (user.Role == "Admin")
                return true;

            // Check direct branch assignment
            if (user.BranchId == branchId)
                return true;

            // Check multiple branch access
            if (user.CanAccessMultipleBranches && user.GetAccessibleBranchIds().Contains(branchId))
                return true;

            return false;
        }

        public async Task<List<BranchAccessDto>> GetAccessibleBranchesForUserAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return new List<BranchAccessDto>();

            var branchIds = new List<int>();

            // Admin can access all branches with full permissions
            if (user.Role == "Admin")
            {
                var allBranches = await _context.Branches
                    .Where(b => b.IsActive)
                    .ToListAsync();

                return allBranches.Select(b => CreateBranchAccessDto(b, user, null, true)).ToList();
            }

            // Add assigned branch
            if (user.BranchId.HasValue)
                branchIds.Add(user.BranchId.Value);

            // Add accessible branches
            if (user.CanAccessMultipleBranches)
                branchIds.AddRange(user.GetAccessibleBranchIds());

            branchIds = branchIds.Distinct().ToList();

            // Get branches with their access permissions
            var branches = await _context.Branches
                .Where(b => branchIds.Contains(b.Id) && b.IsActive)
                .ToListAsync();

            // Get branch access permissions for the user
            var branchAccesses = await _context.BranchAccesses
                .Where(ba => ba.UserId == userId && branchIds.Contains(ba.BranchId) && ba.IsActive)
                .ToListAsync();

            return branches.Select(b => 
            {
                var branchAccess = branchAccesses.FirstOrDefault(ba => ba.BranchId == b.Id);
                return CreateBranchAccessDto(b, user, branchAccess, false);
            }).ToList();
        }

        private BranchAccessDto CreateBranchAccessDto(Branch branch, User user, BranchAccess? branchAccess, bool isAdmin)
        {
            // Determine permissions based on role and branch access
            var permissions = GetUserBranchPermissions(user, branch, branchAccess, isAdmin);

            return new BranchAccessDto
            {
                BranchId = branch.Id,
                BranchName = branch.BranchName,
                BranchCode = branch.BranchCode,
                City = branch.City,
                Province = branch.Province,
                BranchType = branch.BranchType,
                IsActive = branch.IsActive,
                
                // Permission fields
                CanRead = permissions.CanRead,
                CanWrite = permissions.CanWrite,
                CanApprove = permissions.CanApprove,
                CanTransfer = permissions.CanTransfer,
                CanManage = permissions.CanManage,
                
                // Hierarchy and organizational info
                IsHeadOffice = branch.BranchType == BranchType.Head,
                IsDefaultBranch = user.BranchId == branch.Id,
                Level = branch.BranchType == BranchType.Head ? 1 : 2,
                ParentBranchId = branch.BranchType == BranchType.Branch ? 
                    _context.Branches.Where(b => b.BranchType == BranchType.Head && b.IsActive).Select(b => b.Id).FirstOrDefault() : 
                    (int?)null,
                
                // Additional details
                Address = branch.Address,
                ManagerName = branch.ManagerName,
                Phone = branch.Phone,
                CreatedAt = branch.CreatedAt,
                UpdatedAt = branch.UpdatedAt
            };
        }

        private (bool CanRead, bool CanWrite, bool CanApprove, bool CanTransfer, bool CanManage) GetUserBranchPermissions(
            User user, Branch branch, BranchAccess? branchAccess, bool isAdmin)
        {
            // Admin has full permissions
            if (isAdmin || user.Role == "Admin")
            {
                return (true, true, true, true, true);
            }

            // Use explicit branch access permissions if available
            if (branchAccess != null)
            {
                var canManage = DetermineManagementPermission(user, branch);
                return (branchAccess.CanRead, branchAccess.CanWrite, branchAccess.CanApprove, branchAccess.CanTransfer, canManage);
            }

            // Role-based default permissions
            return user.Role.ToUpper() switch
            {
                "HEADMANAGER" => (true, true, true, true, true),
                "BRANCHMANAGER" when user.BranchId == branch.Id => (true, true, true, false, true),
                "BRANCHMANAGER" => (true, false, false, false, false),
                "USER" when user.BranchId == branch.Id => (true, true, false, false, false),
                "USER" => (true, false, false, false, false),
                _ => (true, false, false, false, false)
            };
        }

        private bool DetermineManagementPermission(User user, Branch branch)
        {
            return user.Role.ToUpper() switch
            {
                "ADMIN" => true,
                "HEADMANAGER" => true,
                "BRANCHMANAGER" when user.BranchId == branch.Id => true,
                _ => false
            };
        }

        public async Task<AssignmentResultDto> AutoAssignUserBasedOnRoleAsync(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return new AssignmentResultDto
                    {
                        Success = false,
                        Message = "User not found",
                        ProcessedAt = _timezoneService.Now
                    };
                }

                AssignUserToBranchDto assignmentDto = user.Role.ToUpper() switch
                {
                    "ADMIN" => new AssignUserToBranchDto
                    {
                        UserId = userId,
                        BranchId = null, // Admin tidak assigned ke branch tertentu
                        CanAccessMultipleBranches = true,
                        AccessibleBranchIds = await _context.Branches
                            .Where(b => b.IsActive)
                            .Select(b => b.Id)
                            .ToListAsync()
                    },
                    "HEADMANAGER" => new AssignUserToBranchDto
                    {
                        UserId = userId,
                        BranchId = await _context.Branches
                            .Where(b => b.BranchType == BranchType.Head && b.IsActive)
                            .Select(b => b.Id)
                            .FirstOrDefaultAsync(),
                        CanAccessMultipleBranches = true,
                        AccessibleBranchIds = await _context.Branches
                            .Where(b => b.IsActive)
                            .Select(b => b.Id)
                            .ToListAsync()
                    },
                    "BRANCHMANAGER" => new AssignUserToBranchDto
                    {
                        UserId = userId,
                        BranchId = await _context.Branches
                            .Where(b => b.BranchType == BranchType.Branch && b.IsActive)
                            .OrderBy(b => b.Id)
                            .Select(b => b.Id)
                            .FirstOrDefaultAsync(),
                        CanAccessMultipleBranches = false,
                        AccessibleBranchIds = new List<int>()
                    },
                    "USER" => new AssignUserToBranchDto
                    {
                        UserId = userId,
                        BranchId = await _context.Branches
                            .Where(b => b.BranchType == BranchType.Branch && b.IsActive)
                            .OrderBy(b => b.Id)
                            .Select(b => b.Id)
                            .FirstOrDefaultAsync(),
                        CanAccessMultipleBranches = false,
                        AccessibleBranchIds = new List<int>()
                    },
                    _ => new AssignUserToBranchDto
                    {
                        UserId = userId,
                        BranchId = null,
                        CanAccessMultipleBranches = false,
                        AccessibleBranchIds = new List<int>()
                    }
                };

                return await AssignUserToBranchAsync(assignmentDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error auto-assigning user {UserId} based on role", userId);

                return new AssignmentResultDto
                {
                    Success = false,
                    Message = "An error occurred during auto-assignment",
                    Errors = new List<string> { ex.Message },
                    ProcessedAt = _timezoneService.Now
                };
            }
        }

        public async Task<int> FixNullBranchAssignmentsAsync()
        {
            var usersWithoutBranch = await _context.Users
                .Where(u => u.BranchId == null && u.IsActive)
                .ToListAsync();

            var fixedCount = 0;

            foreach (var user in usersWithoutBranch)
            {
                var result = await AutoAssignUserBasedOnRoleAsync(user.Id);
                if (result.Success)
                {
                    fixedCount++;
                    _logger.LogInformation("Fixed branch assignment for user {UserId} with role {Role}", 
                        user.Id, user.Role);
                }
            }

            return fixedCount;
        }

        private async Task<AssignmentResultDto> ValidateAccessibleBranchesAsync(List<int> branchIds)
        {
            if (branchIds.Any())
            {
                var existingBranches = await _context.Branches
                    .Where(b => branchIds.Contains(b.Id))
                    .Select(b => b.Id)
                    .ToListAsync();

                var invalidBranches = branchIds.Except(existingBranches).ToList();
                if (invalidBranches.Any())
                {
                    return new AssignmentResultDto
                    {
                        Success = false,
                        Message = "Some accessible branches do not exist",
                        Errors = invalidBranches.Select(id => $"Branch ID {id} does not exist").ToList(),
                        ProcessedAt = _timezoneService.Now
                    };
                }
            }

            return new AssignmentResultDto { Success = true, ProcessedAt = _timezoneService.Now };
        }

        private async Task<AssignmentResultDto> ValidateRoleBasedAssignmentAsync(User user, AssignUserToBranchDto dto)
        {
            var errors = new List<string>();

            switch (user.Role.ToUpper())
            {
                case "ADMIN":
                    // Admin can have multi-branch access and typically no specific branch assignment
                    if (dto.BranchId.HasValue && !dto.CanAccessMultipleBranches)
                    {
                        errors.Add("Admin should have multi-branch access enabled when assigned to a specific branch");
                    }
                    break;

                case "HEADMANAGER":
                    // HeadManager should have access to multiple branches
                    if (!dto.CanAccessMultipleBranches)
                    {
                        errors.Add("HeadManager should have multi-branch access enabled");
                    }

                    // If assigned to a branch, it should be Head office
                    if (dto.BranchId.HasValue)
                    {
                        var branch = await _context.Branches.FindAsync(dto.BranchId.Value);
                        if (branch?.BranchType != BranchType.Head)
                        {
                            errors.Add("HeadManager should be assigned to Head office branch");
                        }
                    }
                    break;

                case "BRANCHMANAGER":
                    // BranchManager should be assigned to exactly one branch
                    if (!dto.BranchId.HasValue)
                    {
                        errors.Add("BranchManager must be assigned to a specific branch");
                    }

                    if (dto.CanAccessMultipleBranches && dto.AccessibleBranchIds.Count > 1)
                    {
                        errors.Add("BranchManager should typically manage only one branch");
                    }
                    break;

                case "USER":
                    // Regular users should be assigned to one branch
                    if (!dto.BranchId.HasValue)
                    {
                        errors.Add("User should be assigned to a specific branch");
                    }

                    if (dto.CanAccessMultipleBranches)
                    {
                        errors.Add("Regular users should not have multi-branch access");
                    }
                    break;
            }

            if (errors.Any())
            {
                return new AssignmentResultDto
                {
                    Success = false,
                    Message = "Role-based validation failed",
                    Errors = errors,
                    ProcessedAt = _timezoneService.Now
                };
            }

            return new AssignmentResultDto { Success = true, ProcessedAt = _timezoneService.Now };
        }
    }
}