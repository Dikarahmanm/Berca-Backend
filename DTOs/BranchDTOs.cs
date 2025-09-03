using System.ComponentModel.DataAnnotations;
using Berca_Backend.Models;

namespace Berca_Backend.DTOs
{
    public class BranchDto
    {
        public int Id { get; set; }
        public string BranchCode { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public BranchType BranchType { get; set; }
        public string BranchTypeName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string ManagerName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }

        // Location details
        public string City { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string FullLocationName { get; set; } = string.Empty;

        // Store details
        public DateTime OpeningDate { get; set; }
        public string StoreSize { get; set; } = string.Empty;
        public int EmployeeCount { get; set; }
        public bool IsActive { get; set; }

        // Computed properties
        public bool IsHeadOffice { get; set; }
        public int UserCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateBranchDto
    {
        [Required, MaxLength(20)]
        public string BranchCode { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string BranchName { get; set; } = string.Empty;

        [Required]
        public BranchType BranchType { get; set; }

        [Required, MaxLength(300)]
        public string Address { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string ManagerName { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Email { get; set; }

        // Location details
        [Required, MaxLength(50)]
        public string City { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Province { get; set; } = string.Empty;

        [Required, MaxLength(10)]
        public string PostalCode { get; set; } = string.Empty;

        // Store details
        public DateTime OpeningDate { get; set; } = DateTime.UtcNow; // Will be converted to local by TimezoneService

        [MaxLength(20)]
        public string StoreSize { get; set; } = "Medium";

        public int EmployeeCount { get; set; } = 0;

        public bool IsActive { get; set; } = true;
    }

    public class UpdateBranchDto
    {
        [MaxLength(20)]
        public string? BranchCode { get; set; }

        [MaxLength(100)]
        public string? BranchName { get; set; }

        public BranchType? BranchType { get; set; }

        [MaxLength(300)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? ManagerName { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(100)]
        public string? Email { get; set; }

        // Location updates
        [MaxLength(50)]
        public string? City { get; set; }

        [MaxLength(50)]
        public string? Province { get; set; }

        [MaxLength(10)]
        public string? PostalCode { get; set; }

        // Store updates
        public DateTime? OpeningDate { get; set; }

        [MaxLength(20)]
        public string? StoreSize { get; set; }

        public int? EmployeeCount { get; set; }

        public bool? IsActive { get; set; }
    }

    // NEW: Branch Analytics & Reporting DTOs
    public class BranchPerformanceDto
    {
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;

        // Sales metrics
        public decimal TodaySales { get; set; }
        public decimal WeeklySales { get; set; }
        public decimal MonthlySales { get; set; }
        public int TransactionCount { get; set; }
        public decimal AverageTransactionValue { get; set; }

        // Inventory metrics
        public int TotalProducts { get; set; }
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
        public decimal InventoryValue { get; set; }

        // Operational metrics
        public int ActiveEmployees { get; set; }
        public int MemberCount { get; set; }
        public DateTime LastSaleDate { get; set; }
    }

    public class BranchComparisonDto
    {
        public List<BranchPerformanceDto> Branches { get; set; } = new List<BranchPerformanceDto>();
        public BranchPerformanceDto TotalConsolidated { get; set; } = new BranchPerformanceDto();
        public DateTime ReportDate { get; set; } = DateTime.UtcNow;
    }

    public class CrossBranchInventoryDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public List<BranchStockDto> BranchStocks { get; set; } = new List<BranchStockDto>();
        public int TotalStock { get; set; }
        public decimal AveragePrice { get; set; }
    }

    public class BranchStockDto
    {
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public int Stock { get; set; }
        public decimal SellPrice { get; set; }
        public bool IsLowStock { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class BranchQueryParams
    {
        public string? Search { get; set; }
        public BranchType? BranchType { get; set; }
        public string? City { get; set; }
        public string? Province { get; set; }
        public bool? IsActive { get; set; }
        public string? StoreSize { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; } = "BranchName"; // "BranchName", "City", "OpeningDate"
        public string? SortOrder { get; set; } = "asc"; // "asc", "desc"
    }

    // NEW: User-Branch Assignment DTOs
    public class UserBranchAssignmentDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int? BranchId { get; set; }
        public string? BranchName { get; set; }
        public bool CanAccessMultipleBranches { get; set; }
        public List<int> AccessibleBranchIds { get; set; } = new List<int>();
    }

    public class AssignUserToBranchDto
    {
        [Required]
        public int UserId { get; set; }

        public int? BranchId { get; set; } // nullable untuk unassign

        public bool CanAccessMultipleBranches { get; set; } = false;

        public List<int> AccessibleBranchIds { get; set; } = new List<int>();
    }

    public class BulkAssignUsersToBranchDto
    {
        [Required]
        public List<int> UserIds { get; set; } = new List<int>();

        public int? BranchId { get; set; }

        public bool CanAccessMultipleBranches { get; set; } = false;

        public List<int> AccessibleBranchIds { get; set; } = new List<int>();
    }

    public class UserAssignmentStatusDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int? BranchId { get; set; }
        public string? BranchName { get; set; }
        public string? BranchCity { get; set; }
        public bool CanAccessMultipleBranches { get; set; }
        public List<BranchAccessDto> AccessibleBranches { get; set; } = new List<BranchAccessDto>();
        public bool IsActive { get; set; }
        public DateTime AssignedAt { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class BranchAccessDto
    {
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public string BranchCode { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public BranchType BranchType { get; set; }
        public string BranchTypeName => BranchType.ToString();
        public bool IsActive { get; set; }
        
        // Permission fields
        public bool CanRead { get; set; }
        public bool CanWrite { get; set; }
        public bool CanApprove { get; set; }
        public bool CanTransfer { get; set; }
        public bool CanManage { get; set; }
        
        // Access level computed from permissions
        public string AccessLevel => GetAccessLevel();
        
        // Hierarchy and organizational info
        public bool IsHeadOffice { get; set; }
        public bool IsDefaultBranch { get; set; }
        public int Level { get; set; }
        public int? ParentBranchId { get; set; }
        
        // Additional details
        public string Address { get; set; } = string.Empty;
        public string ManagerName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Helper method to determine access level
        private string GetAccessLevel()
        {
            if (CanManage && CanApprove && CanWrite && CanRead) return "Full";
            if (CanWrite && CanRead) return "Limited";
            if (CanRead) return "ReadOnly";
            return "None";
        }
    }

    public class BranchUserListDto
    {
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public string BranchCode { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public List<UserAssignmentStatusDto> AssignedUsers { get; set; } = new List<UserAssignmentStatusDto>();
        public List<UserAssignmentStatusDto> UsersWithAccess { get; set; } = new List<UserAssignmentStatusDto>();
        public int TotalUserCount { get; set; }
        public int ActiveUserCount { get; set; }
    }

    public class UpdateBranchAccessDto
    {
        [Required]
        public int UserId { get; set; }

        public bool CanAccessMultipleBranches { get; set; }

        [Required]
        public List<int> AccessibleBranchIds { get; set; } = new List<int>();
    }

    public class AssignmentResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new List<string>();
        public UserAssignmentStatusDto? UserAssignment { get; set; }
        public DateTime ProcessedAt { get; set; }
    }

    public class BulkAssignmentResultDto
    {
        public bool OverallSuccess { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<AssignmentResultDto> Results { get; set; } = new List<AssignmentResultDto>();
        public DateTime ProcessedAt { get; set; }
    }

    public class UserBranchQueryParams
    {
        public int? BranchId { get; set; }
        public string? Role { get; set; }
        public bool? CanAccessMultipleBranches { get; set; }
        public bool? IsActive { get; set; }
        public bool? HasBranchAssignment { get; set; }
        public string? Search { get; set; } // Username or full name
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; } = "Username";
        public string? SortOrder { get; set; } = "asc";
    }

    // Additional DTOs for BranchController
    public class BranchDetailDto : BranchDto
    {
        public List<UserAssignmentStatusDto> AssignedUsers { get; set; } = new List<UserAssignmentStatusDto>();
        public List<UserAssignmentStatusDto> UsersWithAccess { get; set; } = new List<UserAssignmentStatusDto>();
        public Dictionary<string, int> UserCountByRole { get; set; } = new Dictionary<string, int>();
        public int TotalActiveUsers { get; set; }
        public int TotalInactiveUsers { get; set; }
        public bool CanEdit { get; set; } // Based on user permissions
        public bool CanDelete { get; set; } // Based on user permissions
    }

    public class BranchUserSummaryDto
    {
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public string BranchCode { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public BranchType BranchType { get; set; }
        public bool IsActive { get; set; }
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public Dictionary<string, int> UserCountByRole { get; set; } = new Dictionary<string, int>();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class BranchHierarchyDto
    {
        public string Region { get; set; } = string.Empty; // Province or geographic grouping
        public List<BranchDto> HeadOffices { get; set; } = new List<BranchDto>();
        public List<BranchDto> RetailBranches { get; set; } = new List<BranchDto>();
        public int TotalBranches { get; set; }
        public int ActiveBranches { get; set; }
        public int TotalEmployees { get; set; }
    }

    public class BranchRegionSummaryDto
    {
        public string Province { get; set; } = string.Empty;
        public List<BranchCitySummaryDto> Cities { get; set; } = new List<BranchCitySummaryDto>();
        public int TotalBranches { get; set; }
        public int ActiveBranches { get; set; }
        public int TotalEmployees { get; set; }
        public DateTime? EarliestOpeningDate { get; set; }
        public DateTime? LatestOpeningDate { get; set; }
    }

    public class BranchCitySummaryDto
    {
        public string City { get; set; } = string.Empty;
        public List<BranchDto> Branches { get; set; } = new List<BranchDto>();
        public int TotalBranches { get; set; }
        public int ActiveBranches { get; set; }
        public int TotalEmployees { get; set; }
    }

    public class BranchAnalyticsDto
    {
        public Dictionary<string, int> BranchCountByStoreSize { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> BranchCountByRegion { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> BranchCountByType { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> UserCountByBranch { get; set; } = new Dictionary<string, int>();
        public int TotalBranches { get; set; }
        public int ActiveBranches { get; set; }
        public int InactiveBranches { get; set; }
        public int TotalEmployees { get; set; }
        public decimal AverageEmployeesPerBranch { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    public class BranchValidationResultDto
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
    }

    public class BranchStatusChangeDto
    {
        [Required]
        public int BranchId { get; set; }
        
        [Required]
        public bool IsActive { get; set; }
        
        public string? Reason { get; set; }
    }

    public class BranchOperationResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new List<string>();
        public BranchDto? Branch { get; set; }
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    }
}