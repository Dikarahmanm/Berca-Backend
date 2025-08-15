using Berca_Backend.Data;
using Berca_Backend.DTOs;
using Berca_Backend.Models;
using Berca_Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Berca_Backend.Services
{
    public class BranchService : IBranchService
    {
        private readonly AppDbContext _context;
        private readonly ITimezoneService _timezoneService;
        private readonly IUserBranchAssignmentService _userBranchService;
        private readonly ILogger<BranchService> _logger;

        public BranchService(
            AppDbContext context,
            ITimezoneService timezoneService,
            IUserBranchAssignmentService userBranchService,
            ILogger<BranchService> logger)
        {
            _context = context;
            _timezoneService = timezoneService;
            _userBranchService = userBranchService;
            _logger = logger;
        }

        public async Task<List<BranchDto>> GetBranchesAsync(BranchQueryParams queryParams, int? userAccessibleBranchesOnly = null)
        {
            var query = _context.Branches.AsQueryable();

            // Apply user access filtering if specified
            if (userAccessibleBranchesOnly.HasValue)
            {
                var accessibleBranchIds = await GetAccessibleBranchIdsAsync(userAccessibleBranchesOnly.Value);
                query = query.Where(b => accessibleBranchIds.Contains(b.Id));
            }

            // Apply filters
            if (!string.IsNullOrEmpty(queryParams.Search))
            {
                var search = queryParams.Search.ToLower();
                query = query.Where(b => b.BranchName.ToLower().Contains(search) ||
                                       b.BranchCode.ToLower().Contains(search) ||
                                       b.City.ToLower().Contains(search) ||
                                       b.Province.ToLower().Contains(search));
            }

            if (queryParams.BranchType.HasValue)
            {
                query = query.Where(b => b.BranchType == queryParams.BranchType.Value);
            }

            if (!string.IsNullOrEmpty(queryParams.City))
            {
                query = query.Where(b => b.City.ToLower() == queryParams.City.ToLower());
            }

            if (!string.IsNullOrEmpty(queryParams.Province))
            {
                query = query.Where(b => b.Province.ToLower() == queryParams.Province.ToLower());
            }

            if (queryParams.IsActive.HasValue)
            {
                query = query.Where(b => b.IsActive == queryParams.IsActive.Value);
            }

            if (!string.IsNullOrEmpty(queryParams.StoreSize))
            {
                query = query.Where(b => b.StoreSize.ToLower() == queryParams.StoreSize.ToLower());
            }

            // Apply sorting
            switch (queryParams.SortBy?.ToLower())
            {
                case "branchname":
                    query = queryParams.SortOrder?.ToLower() == "desc"
                        ? query.OrderByDescending(b => b.BranchName)
                        : query.OrderBy(b => b.BranchName);
                    break;
                case "city":
                    query = queryParams.SortOrder?.ToLower() == "desc"
                        ? query.OrderByDescending(b => b.City)
                        : query.OrderBy(b => b.City);
                    break;
                case "openingdate":
                    query = queryParams.SortOrder?.ToLower() == "desc"
                        ? query.OrderByDescending(b => b.OpeningDate)
                        : query.OrderBy(b => b.OpeningDate);
                    break;
                case "employeecount":
                    query = queryParams.SortOrder?.ToLower() == "desc"
                        ? query.OrderByDescending(b => b.EmployeeCount)
                        : query.OrderBy(b => b.EmployeeCount);
                    break;
                default:
                    query = query.OrderBy(b => b.BranchName);
                    break;
            }

            // Apply pagination
            var skip = (queryParams.Page - 1) * queryParams.PageSize;
            var branches = await query
                .Include(b => b.Users)
                .Skip(skip)
                .Take(queryParams.PageSize)
                .ToListAsync();

            return branches.Select(MapToBranchDto).ToList();
        }

        public async Task<BranchDto?> GetBranchByIdAsync(int id, int? requestingUserId = null)
        {
            // Check access if requesting user is specified
            if (requestingUserId.HasValue && !await CanUserAccessBranchAsync(requestingUserId.Value, id))
            {
                return null;
            }

            var branch = await _context.Branches
                .Include(b => b.Users)
                .FirstOrDefaultAsync(b => b.Id == id);

            return branch != null ? MapToBranchDto(branch) : null;
        }

        public async Task<BranchDto> CreateBranchAsync(CreateBranchDto dto)
        {
            var validationErrors = await ValidateBranchDataAsync(dto);
            if (validationErrors.Any())
            {
                throw new ArgumentException($"Validation failed: {string.Join(", ", validationErrors)}");
            }

            var branch = new Branch
            {
                BranchCode = dto.BranchCode,
                BranchName = dto.BranchName,
                BranchType = dto.BranchType,
                Address = dto.Address,
                ManagerName = dto.ManagerName,
                Phone = dto.Phone,
                Email = dto.Email,
                City = dto.City,
                Province = dto.Province,
                PostalCode = dto.PostalCode,
                OpeningDate = _timezoneService.LocalToUtc(dto.OpeningDate),
                StoreSize = dto.StoreSize,
                EmployeeCount = dto.EmployeeCount,
                IsActive = dto.IsActive,
                CreatedAt = _timezoneService.Now,
                UpdatedAt = _timezoneService.Now
            };

            _context.Branches.Add(branch);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Branch {BranchCode} created successfully with ID {BranchId}", 
                branch.BranchCode, branch.Id);

            return MapToBranchDto(branch);
        }

        public async Task<BranchDto?> UpdateBranchAsync(int id, UpdateBranchDto dto)
        {
            var branch = await _context.Branches.FindAsync(id);
            if (branch == null)
                return null;

            var validationErrors = await ValidateBranchUpdateAsync(id, dto);
            if (validationErrors.Any())
            {
                throw new ArgumentException($"Validation failed: {string.Join(", ", validationErrors)}");
            }

            // Update only provided fields
            if (!string.IsNullOrEmpty(dto.BranchCode))
                branch.BranchCode = dto.BranchCode;
            
            if (!string.IsNullOrEmpty(dto.BranchName))
                branch.BranchName = dto.BranchName;
            
            if (dto.BranchType.HasValue)
                branch.BranchType = dto.BranchType.Value;
            
            if (!string.IsNullOrEmpty(dto.Address))
                branch.Address = dto.Address;
            
            if (!string.IsNullOrEmpty(dto.ManagerName))
                branch.ManagerName = dto.ManagerName;
            
            if (!string.IsNullOrEmpty(dto.Phone))
                branch.Phone = dto.Phone;
            
            if (dto.Email != null)
                branch.Email = dto.Email;
            
            if (!string.IsNullOrEmpty(dto.City))
                branch.City = dto.City;
            
            if (!string.IsNullOrEmpty(dto.Province))
                branch.Province = dto.Province;
            
            if (!string.IsNullOrEmpty(dto.PostalCode))
                branch.PostalCode = dto.PostalCode;
            
            if (dto.OpeningDate.HasValue)
                branch.OpeningDate = _timezoneService.LocalToUtc(dto.OpeningDate.Value);
            
            if (!string.IsNullOrEmpty(dto.StoreSize))
                branch.StoreSize = dto.StoreSize;
            
            if (dto.EmployeeCount.HasValue)
                branch.EmployeeCount = dto.EmployeeCount.Value;
            
            if (dto.IsActive.HasValue)
                branch.IsActive = dto.IsActive.Value;

            branch.UpdatedAt = _timezoneService.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Branch {BranchCode} (ID: {BranchId}) updated successfully", 
                branch.BranchCode, branch.Id);

            return MapToBranchDto(branch);
        }

        public async Task<bool> DeleteBranchAsync(int id)
        {
            var branch = await _context.Branches
                .Include(b => b.Users)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (branch == null)
                return false;

            // Check if branch has users assigned
            if (branch.Users.Any(u => u.IsActive))
            {
                throw new InvalidOperationException("Cannot delete branch with active users assigned. Please reassign users first.");
            }

            _context.Branches.Remove(branch);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Branch {BranchCode} (ID: {BranchId}) deleted permanently", 
                branch.BranchCode, branch.Id);

            return true;
        }

        public async Task<bool> SoftDeleteBranchAsync(int id)
        {
            var branch = await _context.Branches.FindAsync(id);
            if (branch == null)
                return false;

            branch.IsActive = false;
            branch.UpdatedAt = _timezoneService.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Branch {BranchCode} (ID: {BranchId}) soft deleted (deactivated)", 
                branch.BranchCode, branch.Id);

            return true;
        }

        public async Task<List<BranchPerformanceDto>> GetBranchPerformanceAsync(DateTime? startDate = null, DateTime? endDate = null, int? requestingUserId = null)
        {
            var accessibleBranchIds = requestingUserId.HasValue 
                ? await GetAccessibleBranchIdsAsync(requestingUserId.Value)
                : await _context.Branches.Select(b => b.Id).ToListAsync();

            var branches = await _context.Branches
                .Where(b => accessibleBranchIds.Contains(b.Id) && b.IsActive)
                .Include(b => b.Users)
                .ToListAsync();

            var performanceList = new List<BranchPerformanceDto>();

            foreach (var branch in branches)
            {
                // Get sales data (assuming there's a Sales table)
                var salesQuery = _context.Sales.AsQueryable();
                
                // Apply date filters if provided
                if (startDate.HasValue)
                    salesQuery = salesQuery.Where(s => s.SaleDate >= startDate.Value);
                
                if (endDate.HasValue)
                    salesQuery = salesQuery.Where(s => s.SaleDate <= endDate.Value);

                // For now, we'll create mock performance data since sales filtering by branch isn't implemented
                // In a real implementation, you'd filter sales by branch based on cashier or branch assignment
                var performance = new BranchPerformanceDto
                {
                    BranchId = branch.Id,
                    BranchName = branch.BranchName,
                    City = branch.City,
                    Province = branch.Province,
                    TodaySales = 0, // TODO: Calculate from actual sales
                    WeeklySales = 0, // TODO: Calculate from actual sales
                    MonthlySales = 0, // TODO: Calculate from actual sales
                    TransactionCount = 0, // TODO: Calculate from actual sales
                    AverageTransactionValue = 0, // TODO: Calculate from actual sales
                    TotalProducts = await _context.Products.CountAsync(), // This should be branch-specific
                    LowStockCount = 0, // TODO: Calculate from inventory
                    OutOfStockCount = 0, // TODO: Calculate from inventory
                    InventoryValue = 0, // TODO: Calculate from inventory
                    ActiveEmployees = branch.Users.Count(u => u.IsActive),
                    MemberCount = await _context.Members.CountAsync(), // This should be branch-specific
                    LastSaleDate = DateTime.UtcNow // TODO: Get from actual sales
                };

                performanceList.Add(performance);
            }

            return performanceList;
        }

        public async Task<BranchComparisonDto> GetBranchComparisonAsync(DateTime? compareDate = null, int? requestingUserId = null)
        {
            var performanceData = await GetBranchPerformanceAsync(compareDate, compareDate?.AddDays(1), requestingUserId);

            var consolidated = new BranchPerformanceDto
            {
                BranchId = 0,
                BranchName = "Consolidated Total",
                City = "All Cities",
                Province = "All Provinces",
                TodaySales = performanceData.Sum(p => p.TodaySales),
                WeeklySales = performanceData.Sum(p => p.WeeklySales),
                MonthlySales = performanceData.Sum(p => p.MonthlySales),
                TransactionCount = performanceData.Sum(p => p.TransactionCount),
                AverageTransactionValue = performanceData.Average(p => p.AverageTransactionValue),
                TotalProducts = performanceData.Sum(p => p.TotalProducts),
                LowStockCount = performanceData.Sum(p => p.LowStockCount),
                OutOfStockCount = performanceData.Sum(p => p.OutOfStockCount),
                InventoryValue = performanceData.Sum(p => p.InventoryValue),
                ActiveEmployees = performanceData.Sum(p => p.ActiveEmployees),
                MemberCount = performanceData.Sum(p => p.MemberCount)
            };

            return new BranchComparisonDto
            {
                Branches = performanceData,
                TotalConsolidated = consolidated,
                ReportDate = compareDate ?? _timezoneService.Now
            };
        }

        public async Task<List<BranchDto>> GetBranchesByRegionAsync(string? province = null, string? city = null, int? requestingUserId = null)
        {
            var queryParams = new BranchQueryParams
            {
                Province = province,
                City = city,
                Page = 1,
                PageSize = 1000 // Get all matching branches
            };

            return await GetBranchesAsync(queryParams, requestingUserId);
        }

        public async Task<Dictionary<string, List<BranchDto>>> GetBranchHierarchyAsync(int? requestingUserId = null)
        {
            var branches = await GetBranchesAsync(new BranchQueryParams 
            { 
                Page = 1, 
                PageSize = 1000,
                IsActive = true 
            }, requestingUserId);

            var hierarchy = new Dictionary<string, List<BranchDto>>();

            // Group by province
            var groupedByProvince = branches.GroupBy(b => b.Province);

            foreach (var provinceGroup in groupedByProvince)
            {
                var provinceBranches = provinceGroup.ToList();
                
                // Further group by branch type
                var headOffices = provinceBranches.Where(b => b.BranchType == BranchType.Head).ToList();
                var retailBranches = provinceBranches.Where(b => b.BranchType == BranchType.Branch).ToList();

                if (headOffices.Any())
                    hierarchy[$"{provinceGroup.Key} - Head Offices"] = headOffices;
                
                if (retailBranches.Any())
                    hierarchy[$"{provinceGroup.Key} - Retail Branches"] = retailBranches;
            }

            return hierarchy;
        }

        public async Task<Dictionary<string, int>> GetBranchCountByStoreSizeAsync(int? requestingUserId = null)
        {
            var branches = await GetBranchesAsync(new BranchQueryParams 
            { 
                Page = 1, 
                PageSize = 1000,
                IsActive = true 
            }, requestingUserId);

            return branches
                .GroupBy(b => b.StoreSize)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        public async Task<Dictionary<string, int>> GetBranchCountByRegionAsync(int? requestingUserId = null)
        {
            var branches = await GetBranchesAsync(new BranchQueryParams 
            { 
                Page = 1, 
                PageSize = 1000,
                IsActive = true 
            }, requestingUserId);

            return branches
                .GroupBy(b => b.Province)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        public async Task<List<BranchUserSummaryDto>> GetBranchUserSummariesAsync(int? requestingUserId = null)
        {
            var branches = await GetBranchesAsync(new BranchQueryParams 
            { 
                Page = 1, 
                PageSize = 1000 
            }, requestingUserId);

            var summaries = new List<BranchUserSummaryDto>();

            foreach (var branch in branches)
            {
                var branchUsers = await _context.Users
                    .Where(u => u.BranchId == branch.Id)
                    .ToListAsync();

                var userCountByRole = branchUsers
                    .GroupBy(u => u.Role)
                    .ToDictionary(g => g.Key, g => g.Count());

                summaries.Add(new BranchUserSummaryDto
                {
                    BranchId = branch.Id,
                    BranchName = branch.BranchName,
                    BranchCode = branch.BranchCode,
                    City = branch.City,
                    Province = branch.Province,
                    BranchType = branch.BranchType,
                    IsActive = branch.IsActive,
                    TotalUsers = branchUsers.Count,
                    ActiveUsers = branchUsers.Count(u => u.IsActive),
                    UserCountByRole = userCountByRole,
                    CreatedAt = branch.CreatedAt,
                    UpdatedAt = branch.UpdatedAt
                });
            }

            return summaries;
        }

        public async Task<BranchDetailDto> GetBranchDetailWithUsersAsync(int branchId, int? requestingUserId = null)
        {
            if (requestingUserId.HasValue && !await CanUserAccessBranchAsync(requestingUserId.Value, branchId))
            {
                throw new UnauthorizedAccessException("User does not have access to this branch");
            }

            var branch = await _context.Branches
                .Include(b => b.Users)
                    .ThenInclude(u => u.UserProfile)
                .FirstOrDefaultAsync(b => b.Id == branchId);

            if (branch == null)
                throw new ArgumentException("Branch not found");

            var branchDto = MapToBranchDto(branch);
            var branchUserList = await _userBranchService.GetUsersByBranchAsync(branchId);

            var userCountByRole = branch.Users
                .GroupBy(u => u.Role)
                .ToDictionary(g => g.Key, g => g.Count());

            return new BranchDetailDto
            {
                Id = branchDto.Id,
                BranchCode = branchDto.BranchCode,
                BranchName = branchDto.BranchName,
                BranchType = branchDto.BranchType,
                BranchTypeName = branchDto.BranchTypeName,
                Address = branchDto.Address,
                ManagerName = branchDto.ManagerName,
                Phone = branchDto.Phone,
                Email = branchDto.Email,
                City = branchDto.City,
                Province = branchDto.Province,
                PostalCode = branchDto.PostalCode,
                FullLocationName = branchDto.FullLocationName,
                OpeningDate = branchDto.OpeningDate,
                StoreSize = branchDto.StoreSize,
                EmployeeCount = branchDto.EmployeeCount,
                IsActive = branchDto.IsActive,
                IsHeadOffice = branchDto.IsHeadOffice,
                UserCount = branchDto.UserCount,
                CreatedAt = branchDto.CreatedAt,
                UpdatedAt = branchDto.UpdatedAt,
                AssignedUsers = branchUserList.AssignedUsers,
                UsersWithAccess = branchUserList.UsersWithAccess,
                UserCountByRole = userCountByRole,
                TotalActiveUsers = branch.Users.Count(u => u.IsActive),
                TotalInactiveUsers = branch.Users.Count(u => !u.IsActive),
                CanEdit = true, // TODO: Implement based on user permissions
                CanDelete = true // TODO: Implement based on user permissions
            };
        }

        public async Task<List<BranchDto>> GetActiveBranchesAsync(int? requestingUserId = null)
        {
            return await GetBranchesAsync(new BranchQueryParams 
            { 
                Page = 1, 
                PageSize = 1000,
                IsActive = true 
            }, requestingUserId);
        }

        public async Task<List<BranchDto>> GetInactiveBranchesAsync(int? requestingUserId = null)
        {
            return await GetBranchesAsync(new BranchQueryParams 
            { 
                Page = 1, 
                PageSize = 1000,
                IsActive = false 
            }, requestingUserId);
        }

        public async Task<bool> ToggleBranchStatusAsync(int id, bool isActive)
        {
            var branch = await _context.Branches.FindAsync(id);
            if (branch == null)
                return false;

            branch.IsActive = isActive;
            branch.UpdatedAt = _timezoneService.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Branch {BranchCode} (ID: {BranchId}) status changed to {Status}", 
                branch.BranchCode, branch.Id, isActive ? "Active" : "Inactive");

            return true;
        }

        public async Task<bool> IsBranchCodeUniqueAsync(string branchCode, int? excludeBranchId = null)
        {
            var query = _context.Branches.Where(b => b.BranchCode.ToLower() == branchCode.ToLower());
            
            if (excludeBranchId.HasValue)
                query = query.Where(b => b.Id != excludeBranchId.Value);

            return !await query.AnyAsync();
        }

        public async Task<List<int>> GetAccessibleBranchIdsAsync(int userId)
        {
            return await _userBranchService.GetAccessibleBranchesForUserAsync(userId)
                .ContinueWith(t => t.Result.Select(b => b.BranchId).ToList());
        }

        public async Task<bool> CanUserAccessBranchAsync(int userId, int branchId)
        {
            return await _userBranchService.ValidateUserBranchAccessAsync(userId, branchId);
        }

        public async Task<List<string>> ValidateBranchDataAsync(CreateBranchDto dto)
        {
            var errors = new List<string>();

            // Check branch code uniqueness
            if (!await IsBranchCodeUniqueAsync(dto.BranchCode))
            {
                errors.Add($"Branch code '{dto.BranchCode}' is already in use");
            }

            // Validate store size
            var validStoreSizes = new[] { "Small", "Medium", "Large" };
            if (!validStoreSizes.Contains(dto.StoreSize))
            {
                errors.Add($"Store size must be one of: {string.Join(", ", validStoreSizes)}");
            }

            // Validate branch type business rules
            if (dto.BranchType == BranchType.Head)
            {
                var existingHeadOffice = await _context.Branches
                    .Where(b => b.BranchType == BranchType.Head && b.IsActive)
                    .FirstOrDefaultAsync();

                if (existingHeadOffice != null)
                {
                    errors.Add("Only one active Head Office is allowed");
                }
            }

            // Validate opening date
            if (dto.OpeningDate > DateTime.Now)
            {
                errors.Add("Opening date cannot be in the future");
            }

            // Validate employee count
            if (dto.EmployeeCount < 0)
            {
                errors.Add("Employee count cannot be negative");
            }

            return errors;
        }

        public async Task<List<string>> ValidateBranchUpdateAsync(int branchId, UpdateBranchDto dto)
        {
            var errors = new List<string>();

            // Check branch code uniqueness if provided
            if (!string.IsNullOrEmpty(dto.BranchCode))
            {
                if (!await IsBranchCodeUniqueAsync(dto.BranchCode, branchId))
                {
                    errors.Add($"Branch code '{dto.BranchCode}' is already in use");
                }
            }

            // Validate store size if provided
            if (!string.IsNullOrEmpty(dto.StoreSize))
            {
                var validStoreSizes = new[] { "Small", "Medium", "Large" };
                if (!validStoreSizes.Contains(dto.StoreSize))
                {
                    errors.Add($"Store size must be one of: {string.Join(", ", validStoreSizes)}");
                }
            }

            // Validate branch type business rules if changing to Head
            if (dto.BranchType.HasValue && dto.BranchType.Value == BranchType.Head)
            {
                var existingHeadOffice = await _context.Branches
                    .Where(b => b.BranchType == BranchType.Head && b.IsActive && b.Id != branchId)
                    .FirstOrDefaultAsync();

                if (existingHeadOffice != null)
                {
                    errors.Add("Only one active Head Office is allowed");
                }
            }

            // Validate opening date if provided
            if (dto.OpeningDate.HasValue && dto.OpeningDate.Value > DateTime.Now)
            {
                errors.Add("Opening date cannot be in the future");
            }

            // Validate employee count if provided
            if (dto.EmployeeCount.HasValue && dto.EmployeeCount.Value < 0)
            {
                errors.Add("Employee count cannot be negative");
            }

            return errors;
        }

        private BranchDto MapToBranchDto(Branch branch)
        {
            return new BranchDto
            {
                Id = branch.Id,
                BranchCode = branch.BranchCode,
                BranchName = branch.BranchName,
                BranchType = branch.BranchType,
                BranchTypeName = branch.BranchType.ToString(),
                Address = branch.Address,
                ManagerName = branch.ManagerName,
                Phone = branch.Phone,
                Email = branch.Email,
                City = branch.City,
                Province = branch.Province,
                PostalCode = branch.PostalCode,
                FullLocationName = branch.FullLocationName,
                OpeningDate = _timezoneService.UtcToLocal(branch.OpeningDate),
                StoreSize = branch.StoreSize,
                EmployeeCount = branch.EmployeeCount,
                IsActive = branch.IsActive,
                IsHeadOffice = branch.IsHeadOffice,
                UserCount = branch.Users?.Count ?? 0,
                CreatedAt = _timezoneService.UtcToLocal(branch.CreatedAt),
                UpdatedAt = _timezoneService.UtcToLocal(branch.UpdatedAt)
            };
        }
    }
}