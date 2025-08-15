using Berca_Backend.DTOs;

namespace Berca_Backend.Services.Interfaces
{
    public interface IUserBranchAssignmentService
    {
        Task<AssignmentResultDto> AssignUserToBranchAsync(AssignUserToBranchDto dto);
        Task<BulkAssignmentResultDto> BulkAssignUsersToBranchAsync(BulkAssignUsersToBranchDto dto);
        Task<AssignmentResultDto> UpdateBranchAccessAsync(UpdateBranchAccessDto dto);
        Task<UserAssignmentStatusDto?> GetUserAssignmentStatusAsync(int userId);
        Task<BranchUserListDto> GetUsersByBranchAsync(int branchId);
        Task<List<UserAssignmentStatusDto>> GetUsersWithQueryAsync(UserBranchQueryParams queryParams);
        Task<AssignmentResultDto> UnassignUserFromBranchAsync(int userId);
        Task<List<UserAssignmentStatusDto>> GetUnassignedUsersAsync();
        Task<bool> ValidateUserBranchAccessAsync(int userId, int branchId);
        Task<List<BranchAccessDto>> GetAccessibleBranchesForUserAsync(int userId);
        Task<AssignmentResultDto> AutoAssignUserBasedOnRoleAsync(int userId);
        Task<int> FixNullBranchAssignmentsAsync();
    }
}