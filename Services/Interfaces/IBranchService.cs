using Berca_Backend.DTOs;

namespace Berca_Backend.Services.Interfaces
{
    public interface IBranchService
    {
        // Basic CRUD Operations
        Task<List<BranchDto>> GetBranchesAsync(BranchQueryParams queryParams, int? userAccessibleBranchesOnly = null);
        Task<BranchDto?> GetBranchByIdAsync(int id, int? requestingUserId = null);
        Task<BranchDto> CreateBranchAsync(CreateBranchDto dto);
        Task<BranchDto?> UpdateBranchAsync(int id, UpdateBranchDto dto);
        Task<bool> DeleteBranchAsync(int id);
        Task<bool> SoftDeleteBranchAsync(int id);

        // Retail Chain Specific Features
        Task<List<BranchPerformanceDto>> GetBranchPerformanceAsync(DateTime? startDate = null, DateTime? endDate = null, int? requestingUserId = null);
        Task<BranchComparisonDto> GetBranchComparisonAsync(DateTime? compareDate = null, int? requestingUserId = null);
        Task<List<BranchDto>> GetBranchesByRegionAsync(string? province = null, string? city = null, int? requestingUserId = null);
        Task<Dictionary<string, List<BranchDto>>> GetBranchHierarchyAsync(int? requestingUserId = null);

        // Analytics and Metrics
        Task<Dictionary<string, int>> GetBranchCountByStoreSizeAsync(int? requestingUserId = null);
        Task<Dictionary<string, int>> GetBranchCountByRegionAsync(int? requestingUserId = null);
        Task<List<BranchUserSummaryDto>> GetBranchUserSummariesAsync(int? requestingUserId = null);
        Task<BranchDetailDto> GetBranchDetailWithUsersAsync(int branchId, int? requestingUserId = null);

        // Store Operations
        Task<List<BranchDto>> GetActiveBranchesAsync(int? requestingUserId = null);
        Task<List<BranchDto>> GetInactiveBranchesAsync(int? requestingUserId = null);
        Task<bool> ToggleBranchStatusAsync(int id, bool isActive);

        // Validation and Business Logic
        Task<bool> IsBranchCodeUniqueAsync(string branchCode, int? excludeBranchId = null);
        Task<List<int>> GetAccessibleBranchIdsAsync(int userId);
        Task<bool> CanUserAccessBranchAsync(int userId, int branchId);
        Task<List<string>> ValidateBranchDataAsync(CreateBranchDto dto);
        Task<List<string>> ValidateBranchUpdateAsync(int branchId, UpdateBranchDto dto);
    }
}