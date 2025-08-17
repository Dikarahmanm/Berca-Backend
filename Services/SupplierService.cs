using Berca_Backend.Data;
using Berca_Backend.DTOs;
using Berca_Backend.Models;
using Berca_Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Berca_Backend.Services
{
    /// <summary>
    /// Supplier Management Service implementation
    /// Handles all supplier operations with branch integration and audit trail
    /// </summary>
    public class SupplierService : ISupplierService
    {
        private readonly AppDbContext _context;
        private readonly ITimezoneService _timezoneService;
        private readonly ILogger<SupplierService> _logger;

        public SupplierService(
            AppDbContext context,
            ITimezoneService timezoneService,
            ILogger<SupplierService> logger)
        {
            _context = context;
            _timezoneService = timezoneService;
            _logger = logger;
        }

        // ==================== CRUD OPERATIONS ==================== //

        public async Task<SupplierPagedResponseDto> GetSuppliersAsync(SupplierQueryParams queryParams, int requestingUserId)
        {
            try
            {
                // Get user's accessible branches
                var user = await _context.Users.FindAsync(requestingUserId);
                var accessibleBranchIds = GetAccessibleBranchIds(user);

                var query = _context.Suppliers
                    .Include(s => s.Branch)
                    .Include(s => s.CreatedByUser)
                    .AsQueryable();

                // Apply branch access control
                if (accessibleBranchIds != null)
                {
                    query = query.Where(s => s.BranchId == null || accessibleBranchIds.Contains(s.BranchId.Value));
                }

                // Apply filters
                if (!string.IsNullOrEmpty(queryParams.Search))
                {
                    var searchLower = queryParams.Search.ToLower();
                    query = query.Where(s =>
                        s.CompanyName.ToLower().Contains(searchLower) ||
                        s.SupplierCode.ToLower().Contains(searchLower) ||
                        s.ContactPerson.ToLower().Contains(searchLower) ||
                        s.Phone.Contains(queryParams.Search) ||
                        s.Email.ToLower().Contains(searchLower));
                }

                if (queryParams.BranchId.HasValue)
                {
                    query = query.Where(s => s.BranchId == queryParams.BranchId.Value);
                }

                if (queryParams.IsActive.HasValue)
                {
                    query = query.Where(s => s.IsActive == queryParams.IsActive.Value);
                }

                if (queryParams.MinPaymentTerms.HasValue)
                {
                    query = query.Where(s => s.PaymentTerms >= queryParams.MinPaymentTerms.Value);
                }

                if (queryParams.MaxPaymentTerms.HasValue)
                {
                    query = query.Where(s => s.PaymentTerms <= queryParams.MaxPaymentTerms.Value);
                }

                if (queryParams.MinCreditLimit.HasValue)
                {
                    query = query.Where(s => s.CreditLimit >= queryParams.MinCreditLimit.Value);
                }

                if (queryParams.MaxCreditLimit.HasValue)
                {
                    query = query.Where(s => s.CreditLimit <= queryParams.MaxCreditLimit.Value);
                }

                // Apply sorting
                query = queryParams.SortBy.ToLower() switch
                {
                    "suppliercode" => queryParams.SortOrder.ToLower() == "desc" 
                        ? query.OrderByDescending(s => s.SupplierCode)
                        : query.OrderBy(s => s.SupplierCode),
                    "contactperson" => queryParams.SortOrder.ToLower() == "desc"
                        ? query.OrderByDescending(s => s.ContactPerson)
                        : query.OrderBy(s => s.ContactPerson),
                    "paymentterms" => queryParams.SortOrder.ToLower() == "desc"
                        ? query.OrderByDescending(s => s.PaymentTerms)
                        : query.OrderBy(s => s.PaymentTerms),
                    "creditlimit" => queryParams.SortOrder.ToLower() == "desc"
                        ? query.OrderByDescending(s => s.CreditLimit)
                        : query.OrderBy(s => s.CreditLimit),
                    "createdat" => queryParams.SortOrder.ToLower() == "desc"
                        ? query.OrderByDescending(s => s.CreatedAt)
                        : query.OrderBy(s => s.CreatedAt),
                    _ => queryParams.SortOrder.ToLower() == "desc"
                        ? query.OrderByDescending(s => s.CompanyName)
                        : query.OrderBy(s => s.CompanyName)
                };

                // Get total count
                var totalCount = await query.CountAsync();

                // Apply pagination
                var suppliers = await query
                    .Skip((queryParams.Page - 1) * queryParams.PageSize)
                    .Take(queryParams.PageSize)
                    .Select(s => new SupplierListDto
                    {
                        Id = s.Id,
                        SupplierCode = s.SupplierCode,
                        CompanyName = s.CompanyName,
                        ContactPerson = s.ContactPerson,
                        Phone = s.Phone,
                        Email = s.Email,
                        PaymentTerms = s.PaymentTerms,
                        CreditLimit = s.CreditLimit,
                        IsActive = s.IsActive,
                        BranchName = s.Branch != null ? s.Branch.BranchName : "Semua Cabang",
                        StatusDisplay = s.IsActive ? "Aktif" : "Tidak Aktif",
                        PaymentTermsDisplay = s.PaymentTerms + " hari",
                        CreditLimitDisplay = s.CreditLimit.ToString("C", new CultureInfo("id-ID")),
                        CreatedAt = s.CreatedAt
                    })
                    .ToListAsync();

                var totalPages = (int)Math.Ceiling((double)totalCount / queryParams.PageSize);

                return new SupplierPagedResponseDto
                {
                    Suppliers = suppliers,
                    TotalCount = totalCount,
                    Page = queryParams.Page,
                    PageSize = queryParams.PageSize,
                    TotalPages = totalPages,
                    HasNextPage = queryParams.Page < totalPages,
                    HasPreviousPage = queryParams.Page > 1
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving suppliers for user {UserId}", requestingUserId);
                throw;
            }
        }

        public async Task<SupplierDto?> GetSupplierByIdAsync(int supplierId, int requestingUserId)
        {
            try
            {
                var user = await _context.Users.FindAsync(requestingUserId);
                var accessibleBranchIds = GetAccessibleBranchIds(user);

                var supplier = await _context.Suppliers
                    .Include(s => s.Branch)
                    .Include(s => s.CreatedByUser)
                    .Include(s => s.UpdatedByUser)
                    .FirstOrDefaultAsync(s => s.Id == supplierId);

                if (supplier == null)
                    return null;

                // Check branch access
                if (accessibleBranchIds != null && supplier.BranchId.HasValue && 
                    !accessibleBranchIds.Contains(supplier.BranchId.Value))
                {
                    _logger.LogWarning("User {UserId} attempted to access supplier {SupplierId} from inaccessible branch {BranchId}", 
                        requestingUserId, supplierId, supplier.BranchId);
                    return null;
                }

                return MapToSupplierDto(supplier);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving supplier {SupplierId} for user {UserId}", supplierId, requestingUserId);
                throw;
            }
        }

        public async Task<SupplierDto?> GetSupplierByCodeAsync(string supplierCode, int requestingUserId)
        {
            try
            {
                var user = await _context.Users.FindAsync(requestingUserId);
                var accessibleBranchIds = GetAccessibleBranchIds(user);

                var supplier = await _context.Suppliers
                    .Include(s => s.Branch)
                    .Include(s => s.CreatedByUser)
                    .Include(s => s.UpdatedByUser)
                    .FirstOrDefaultAsync(s => s.SupplierCode == supplierCode);

                if (supplier == null)
                    return null;

                // Check branch access
                if (accessibleBranchIds != null && supplier.BranchId.HasValue && 
                    !accessibleBranchIds.Contains(supplier.BranchId.Value))
                {
                    return null;
                }

                return MapToSupplierDto(supplier);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving supplier by code {SupplierCode} for user {UserId}", supplierCode, requestingUserId);
                throw;
            }
        }

        public async Task<SupplierDto> CreateSupplierAsync(CreateSupplierDto createDto, int createdByUserId)
        {
            try
            {
                // Validate business rules
                if (!await IsCompanyNameUniqueAsync(createDto.CompanyName, createDto.BranchId))
                {
                    throw new InvalidOperationException($"Company name '{createDto.CompanyName}' already exists in the specified branch.");
                }

                if (!await IsEmailUniqueAsync(createDto.Email))
                {
                    throw new InvalidOperationException($"Email '{createDto.Email}' is already in use.");
                }

                // Validate branch access if specified
                if (createDto.BranchId.HasValue)
                {
                    var branch = await _context.Branches.FindAsync(createDto.BranchId.Value);
                    if (branch == null || !branch.IsActive)
                    {
                        throw new InvalidOperationException("Invalid or inactive branch specified.");
                    }
                }

                // Generate unique supplier code
                var supplierCode = await GenerateSupplierCodeAsync();

                var supplier = new Supplier
                {
                    SupplierCode = supplierCode,
                    CompanyName = createDto.CompanyName,
                    ContactPerson = createDto.ContactPerson,
                    Phone = createDto.Phone,
                    Email = createDto.Email,
                    Address = createDto.Address,
                    PaymentTerms = createDto.PaymentTerms,
                    CreditLimit = createDto.CreditLimit,
                    BranchId = createDto.BranchId,
                    IsActive = createDto.IsActive,
                    CreatedBy = createdByUserId,
                    CreatedAt = _timezoneService.UtcToLocal(DateTime.UtcNow),
                    UpdatedAt = _timezoneService.UtcToLocal(DateTime.UtcNow)
                };

                _context.Suppliers.Add(supplier);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Supplier {SupplierCode} created by user {UserId}", supplier.SupplierCode, createdByUserId);

                // Reload with navigation properties
                await _context.Entry(supplier)
                    .Reference(s => s.Branch)
                    .LoadAsync();
                await _context.Entry(supplier)
                    .Reference(s => s.CreatedByUser)
                    .LoadAsync();

                return MapToSupplierDto(supplier);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating supplier for user {UserId}", createdByUserId);
                throw;
            }
        }

        public async Task<SupplierDto?> UpdateSupplierAsync(int supplierId, UpdateSupplierDto updateDto, int updatedByUserId)
        {
            try
            {
                var supplier = await _context.Suppliers
                    .Include(s => s.Branch)
                    .Include(s => s.CreatedByUser)
                    .FirstOrDefaultAsync(s => s.Id == supplierId);

                if (supplier == null)
                    return null;

                // Validate business rules
                if (!await IsCompanyNameUniqueAsync(updateDto.CompanyName, updateDto.BranchId, supplierId))
                {
                    throw new InvalidOperationException($"Company name '{updateDto.CompanyName}' already exists in the specified branch.");
                }

                if (!await IsEmailUniqueAsync(updateDto.Email, supplierId))
                {
                    throw new InvalidOperationException($"Email '{updateDto.Email}' is already in use.");
                }

                // Validate branch access if specified
                if (updateDto.BranchId.HasValue)
                {
                    var branch = await _context.Branches.FindAsync(updateDto.BranchId.Value);
                    if (branch == null || !branch.IsActive)
                    {
                        throw new InvalidOperationException("Invalid or inactive branch specified.");
                    }
                }

                // Update fields
                supplier.CompanyName = updateDto.CompanyName;
                supplier.ContactPerson = updateDto.ContactPerson;
                supplier.Phone = updateDto.Phone;
                supplier.Email = updateDto.Email;
                supplier.Address = updateDto.Address;
                supplier.PaymentTerms = updateDto.PaymentTerms;
                supplier.CreditLimit = updateDto.CreditLimit;
                supplier.BranchId = updateDto.BranchId;
                supplier.IsActive = updateDto.IsActive;
                supplier.UpdatedBy = updatedByUserId;
                supplier.UpdatedAt = _timezoneService.UtcToLocal(DateTime.UtcNow);

                await _context.SaveChangesAsync();

                _logger.LogInformation("Supplier {SupplierId} updated by user {UserId}", supplierId, updatedByUserId);

                // Reload updated by user
                await _context.Entry(supplier)
                    .Reference(s => s.UpdatedByUser)
                    .LoadAsync();

                return MapToSupplierDto(supplier);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating supplier {SupplierId} for user {UserId}", supplierId, updatedByUserId);
                throw;
            }
        }

        public async Task<bool> DeleteSupplierAsync(int supplierId, int deletedByUserId, string? reason = null)
        {
            try
            {
                var supplier = await _context.Suppliers.FindAsync(supplierId);
                if (supplier == null)
                    return false;

                // Soft delete
                supplier.IsActive = false;
                supplier.UpdatedBy = deletedByUserId;
                supplier.UpdatedAt = _timezoneService.UtcToLocal(DateTime.UtcNow);

                await _context.SaveChangesAsync();

                _logger.LogInformation("Supplier {SupplierId} deleted by user {UserId}. Reason: {Reason}", 
                    supplierId, deletedByUserId, reason ?? "Not specified");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting supplier {SupplierId} for user {UserId}", supplierId, deletedByUserId);
                throw;
            }
        }

        public async Task<SupplierDto?> ToggleSupplierStatusAsync(int supplierId, bool isActive, int updatedByUserId, string? reason = null)
        {
            try
            {
                var supplier = await _context.Suppliers
                    .Include(s => s.Branch)
                    .Include(s => s.CreatedByUser)
                    .FirstOrDefaultAsync(s => s.Id == supplierId);

                if (supplier == null)
                    return null;

                supplier.IsActive = isActive;
                supplier.UpdatedBy = updatedByUserId;
                supplier.UpdatedAt = _timezoneService.UtcToLocal(DateTime.UtcNow);

                await _context.SaveChangesAsync();

                _logger.LogInformation("Supplier {SupplierId} status changed to {Status} by user {UserId}. Reason: {Reason}",
                    supplierId, isActive ? "Active" : "Inactive", updatedByUserId, reason ?? "Not specified");

                // Reload updated by user
                await _context.Entry(supplier)
                    .Reference(s => s.UpdatedByUser)
                    .LoadAsync();

                return MapToSupplierDto(supplier);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling supplier {SupplierId} status for user {UserId}", supplierId, updatedByUserId);
                throw;
            }
        }

        // ==================== BRANCH-SPECIFIC OPERATIONS ==================== //

        public async Task<List<SupplierSummaryDto>> GetSuppliersByBranchAsync(int branchId, bool includeAll = true, bool activeOnly = true)
        {
            try
            {
                var query = _context.Suppliers
                    .Include(s => s.Branch)
                    .AsQueryable();

                if (includeAll)
                {
                    query = query.Where(s => s.BranchId == branchId || s.BranchId == null);
                }
                else
                {
                    query = query.Where(s => s.BranchId == branchId);
                }

                if (activeOnly)
                {
                    query = query.Where(s => s.IsActive);
                }

                return await query
                    .Select(s => new SupplierSummaryDto
                    {
                        Id = s.Id,
                        SupplierCode = s.SupplierCode,
                        CompanyName = s.CompanyName,
                        ContactPerson = s.ContactPerson,
                        Phone = s.Phone,
                        IsActive = s.IsActive,
                        PaymentTerms = s.PaymentTerms,
                        CreditLimit = s.CreditLimit,
                        BranchName = s.Branch != null ? s.Branch.BranchName : "Semua Cabang"
                    })
                    .OrderBy(s => s.CompanyName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving suppliers for branch {BranchId}", branchId);
                throw;
            }
        }

        public async Task<List<SupplierSummaryDto>> GetActiveSuppliersByPaymentTermsAsync(int minDays, int maxDays, int? branchId = null)
        {
            try
            {
                var query = _context.Suppliers
                    .Include(s => s.Branch)
                    .Where(s => s.IsActive && s.PaymentTerms >= minDays && s.PaymentTerms <= maxDays);

                if (branchId.HasValue)
                {
                    query = query.Where(s => s.BranchId == branchId.Value || s.BranchId == null);
                }

                return await query
                    .Select(s => new SupplierSummaryDto
                    {
                        Id = s.Id,
                        SupplierCode = s.SupplierCode,
                        CompanyName = s.CompanyName,
                        ContactPerson = s.ContactPerson,
                        Phone = s.Phone,
                        IsActive = s.IsActive,
                        PaymentTerms = s.PaymentTerms,
                        CreditLimit = s.CreditLimit,
                        BranchName = s.Branch != null ? s.Branch.BranchName : "Semua Cabang"
                    })
                    .OrderBy(s => s.PaymentTerms)
                    .ThenBy(s => s.CompanyName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving suppliers by payment terms {MinDays}-{MaxDays} for branch {BranchId}", minDays, maxDays, branchId);
                throw;
            }
        }

        public async Task<List<SupplierSummaryDto>> GetSuppliersByCreditLimitAsync(decimal minCreditLimit, int? branchId = null)
        {
            try
            {
                var query = _context.Suppliers
                    .Include(s => s.Branch)
                    .Where(s => s.IsActive && s.CreditLimit >= minCreditLimit);

                if (branchId.HasValue)
                {
                    query = query.Where(s => s.BranchId == branchId.Value || s.BranchId == null);
                }

                return await query
                    .Select(s => new SupplierSummaryDto
                    {
                        Id = s.Id,
                        SupplierCode = s.SupplierCode,
                        CompanyName = s.CompanyName,
                        ContactPerson = s.ContactPerson,
                        Phone = s.Phone,
                        IsActive = s.IsActive,
                        PaymentTerms = s.PaymentTerms,
                        CreditLimit = s.CreditLimit,
                        BranchName = s.Branch != null ? s.Branch.BranchName : "Semua Cabang"
                    })
                    .OrderByDescending(s => s.CreditLimit)
                    .ThenBy(s => s.CompanyName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving suppliers by credit limit {MinCreditLimit} for branch {BranchId}", minCreditLimit, branchId);
                throw;
            }
        }

        // ==================== VALIDATION & BUSINESS LOGIC ==================== //

        public async Task<bool> IsSupplierCodeUniqueAsync(string supplierCode, int? excludeSupplierId = null)
        {
            try
            {
                var query = _context.Suppliers.Where(s => s.SupplierCode == supplierCode);
                
                if (excludeSupplierId.HasValue)
                {
                    query = query.Where(s => s.Id != excludeSupplierId.Value);
                }

                return !await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking supplier code uniqueness for {SupplierCode}", supplierCode);
                throw;
            }
        }

        public async Task<bool> IsCompanyNameUniqueAsync(string companyName, int? branchId, int? excludeSupplierId = null)
        {
            try
            {
                var query = _context.Suppliers.Where(s => s.CompanyName.ToLower() == companyName.ToLower());

                // Check within same branch (null branch means global)
                if (branchId.HasValue)
                {
                    query = query.Where(s => s.BranchId == branchId.Value);
                }
                else
                {
                    query = query.Where(s => s.BranchId == null);
                }

                if (excludeSupplierId.HasValue)
                {
                    query = query.Where(s => s.Id != excludeSupplierId.Value);
                }

                return !await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking company name uniqueness for {CompanyName} in branch {BranchId}", companyName, branchId);
                throw;
            }
        }

        public async Task<bool> IsEmailUniqueAsync(string email, int? excludeSupplierId = null)
        {
            try
            {
                var query = _context.Suppliers.Where(s => s.Email.ToLower() == email.ToLower());
                
                if (excludeSupplierId.HasValue)
                {
                    query = query.Where(s => s.Id != excludeSupplierId.Value);
                }

                return !await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email uniqueness for {Email}", email);
                throw;
            }
        }

        public async Task<string> GenerateSupplierCodeAsync()
        {
            try
            {
                var year = DateTime.Now.Year;
                var prefix = $"SUP-{year}-";

                // Get the last supplier code for this year
                var lastSupplier = await _context.Suppliers
                    .Where(s => s.SupplierCode.StartsWith(prefix))
                    .OrderByDescending(s => s.SupplierCode)
                    .FirstOrDefaultAsync();

                int nextNumber = 1;
                if (lastSupplier != null)
                {
                    var lastCodePart = lastSupplier.SupplierCode.Substring(prefix.Length);
                    if (int.TryParse(lastCodePart, out var lastNumber))
                    {
                        nextNumber = lastNumber + 1;
                    }
                }

                var supplierCode = $"{prefix}{nextNumber:D5}";

                // Ensure uniqueness
                while (await _context.Suppliers.AnyAsync(s => s.SupplierCode == supplierCode))
                {
                    nextNumber++;
                    supplierCode = $"{prefix}{nextNumber:D5}";
                }

                return supplierCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating supplier code");
                throw;
            }
        }

        // ==================== REPORTING & ANALYTICS ==================== //

        public async Task<SupplierStatsDto> GetSupplierStatsAsync(int? branchId = null)
        {
            try
            {
                var query = _context.Suppliers.AsQueryable();

                if (branchId.HasValue)
                {
                    query = query.Where(s => s.BranchId == branchId.Value || s.BranchId == null);
                }

                var totalSuppliers = await query.CountAsync();
                var activeSuppliers = await query.CountAsync(s => s.IsActive);
                var inactiveSuppliers = totalSuppliers - activeSuppliers;
                var totalCreditLimit = await query.SumAsync(s => s.CreditLimit);
                var averagePaymentTerms = await query.AverageAsync(s => (double)s.PaymentTerms);
                var suppliersWithCreditLimit = await query.CountAsync(s => s.CreditLimit > 0);
                var shortTermSuppliers = await query.CountAsync(s => s.PaymentTerms <= 7);
                var longTermSuppliers = await query.CountAsync(s => s.PaymentTerms >= 60);

                return new SupplierStatsDto
                {
                    TotalSuppliers = totalSuppliers,
                    ActiveSuppliers = activeSuppliers,
                    InactiveSuppliers = inactiveSuppliers,
                    TotalCreditLimit = totalCreditLimit,
                    AveragePaymentTerms = (decimal)averagePaymentTerms,
                    SuppliersWithCreditLimit = suppliersWithCreditLimit,
                    ShortTermSuppliers = shortTermSuppliers,
                    LongTermSuppliers = longTermSuppliers
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving supplier stats for branch {BranchId}", branchId);
                throw;
            }
        }

        public async Task<List<SupplierAlertDto>> GetSuppliersRequiringAttentionAsync(int? branchId = null)
        {
            try
            {
                var alerts = new List<SupplierAlertDto>();
                var query = _context.Suppliers.AsQueryable();

                if (branchId.HasValue)
                {
                    query = query.Where(s => s.BranchId == branchId.Value || s.BranchId == null);
                }

                // Inactive suppliers
                var inactiveSuppliers = await query
                    .Where(s => !s.IsActive)
                    .Select(s => new SupplierAlertDto
                    {
                        Id = s.Id,
                        SupplierCode = s.SupplierCode,
                        CompanyName = s.CompanyName,
                        AlertType = "Inactive",
                        AlertMessage = "Supplier is currently inactive",
                        Severity = "Medium",
                        CreatedAt = s.UpdatedAt
                    })
                    .ToListAsync();

                // Long payment terms
                var longTermSuppliers = await query
                    .Where(s => s.IsActive && s.PaymentTerms >= 90)
                    .Select(s => new SupplierAlertDto
                    {
                        Id = s.Id,
                        SupplierCode = s.SupplierCode,
                        CompanyName = s.CompanyName,
                        AlertType = "Long Payment Terms",
                        AlertMessage = $"Payment terms of {s.PaymentTerms} days may affect cash flow",
                        Severity = "Low",
                        CreatedAt = s.CreatedAt
                    })
                    .ToListAsync();

                // High credit limit
                var highCreditSuppliers = await query
                    .Where(s => s.IsActive && s.CreditLimit >= 100000000) // 100M IDR
                    .Select(s => new SupplierAlertDto
                    {
                        Id = s.Id,
                        SupplierCode = s.SupplierCode,
                        CompanyName = s.CompanyName,
                        AlertType = "High Credit Limit",
                        AlertMessage = $"High credit limit of {s.CreditLimit:C} requires monitoring",
                        Severity = "High",
                        CreatedAt = s.CreatedAt
                    })
                    .ToListAsync();

                alerts.AddRange(inactiveSuppliers);
                alerts.AddRange(longTermSuppliers);
                alerts.AddRange(highCreditSuppliers);

                return alerts.OrderByDescending(a => a.Severity).ThenBy(a => a.CompanyName).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving suppliers requiring attention for branch {BranchId}", branchId);
                throw;
            }
        }

        // ==================== HELPER METHODS ==================== //

        private static List<int>? GetAccessibleBranchIds(User? user)
        {
            if (user?.Role == "Admin")
                return null; // Admin can access all

            var branchIds = new List<int>();
            if (user?.BranchId.HasValue == true)
                branchIds.Add(user.BranchId.Value);

            if (!string.IsNullOrEmpty(user?.AccessibleBranchIds))
            {
                var additionalIds = user.AccessibleBranchIds
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(int.Parse)
                    .ToList();
                branchIds.AddRange(additionalIds);
            }

            return branchIds.Distinct().ToList();
        }

        private static SupplierDto MapToSupplierDto(Supplier supplier)
        {
            return new SupplierDto
            {
                Id = supplier.Id,
                SupplierCode = supplier.SupplierCode,
                CompanyName = supplier.CompanyName,
                ContactPerson = supplier.ContactPerson,
                Phone = supplier.Phone,
                Email = supplier.Email,
                Address = supplier.Address,
                PaymentTerms = supplier.PaymentTerms,
                CreditLimit = supplier.CreditLimit,
                IsActive = supplier.IsActive,
                BranchId = supplier.BranchId,
                BranchName = supplier.Branch?.BranchName ?? "",
                BranchCode = supplier.Branch?.BranchCode ?? "",
                CreatedBy = supplier.CreatedBy,
                CreatedByName = supplier.CreatedByUser?.Username ?? "",
                UpdatedBy = supplier.UpdatedBy,
                UpdatedByName = supplier.UpdatedByUser?.Username ?? "",
                CreatedAt = supplier.CreatedAt,
                UpdatedAt = supplier.UpdatedAt,
                StatusDisplay = supplier.StatusDisplay,
                PaymentTermsDisplay = supplier.PaymentTermsDisplay,
                CreditLimitDisplay = supplier.CreditLimitDisplay,
                BranchDisplay = supplier.BranchDisplay,
                HasCreditLimit = supplier.HasCreditLimit,
                IsShortPaymentTerm = supplier.IsShortPaymentTerm,
                IsLongPaymentTerm = supplier.IsLongPaymentTerm
            };
        }
    }
}