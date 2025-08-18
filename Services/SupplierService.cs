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

        // ==================== PAYMENT TRACKING INTEGRATION ==================== //

        public async Task<SupplierPaymentHistoryDto> GetSupplierPaymentHistoryAsync(int supplierId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var supplier = await _context.Suppliers.FindAsync(supplierId);
                if (supplier == null)
                    throw new InvalidOperationException($"Supplier with ID {supplierId} not found.");

                var startDate = fromDate ?? DateTime.UtcNow.AddMonths(-12);
                var endDate = toDate ?? DateTime.UtcNow;

                // Get factures for the supplier within date range
                var factures = await _context.Factures
                    .Include(f => f.Payments)
                    .Where(f => f.SupplierId == supplierId && 
                               f.InvoiceDate >= startDate && 
                               f.InvoiceDate <= endDate)
                    .ToListAsync();

                var totalFactures = factures.Count;
                var paidFactures = factures.Count(f => f.Status == FactureStatus.Paid);
                var pendingFactures = factures.Count(f => f.Status != FactureStatus.Paid && f.Status != FactureStatus.Cancelled);
                var overdueFactures = factures.Count(f => f.IsOverdue);

                var totalInvoiced = factures.Sum(f => f.TotalAmount);
                var totalPaid = factures.Sum(f => f.PaidAmount);
                var totalOutstanding = totalInvoiced - totalPaid;

                // Calculate average payment days
                var completedPayments = factures
                    .Where(f => f.Status == FactureStatus.Paid && f.Payments.Any())
                    .SelectMany(f => f.Payments.Where(p => p.Status == PaymentStatus.Confirmed))
                    .ToList();

                var averagePaymentDays = completedPayments.Any()
                    ? completedPayments.Average(p => (p.PaymentDate - p.Facture!.InvoiceDate).Days)
                    : 0;

                // Get recent payments
                var recentPayments = factures
                    .SelectMany(f => f.Payments.Where(p => p.Status == PaymentStatus.Confirmed))
                    .OrderByDescending(p => p.PaymentDate)
                    .Take(10)
                    .Select(p => new FacturePaymentSummaryDto
                    {
                        FactureId = p.FactureId,
                        InternalReferenceNumber = p.Facture!.InternalReferenceNumber,
                        SupplierInvoiceNumber = p.Facture.SupplierInvoiceNumber,
                        PaymentDate = p.PaymentDate,
                        Amount = p.Amount,
                        AmountDisplay = p.Amount.ToString("C", new CultureInfo("id-ID")),
                        PaymentMethod = p.PaymentMethod.ToString(),
                        Status = p.Status.ToString(),
                        OurPaymentReference = p.OurPaymentReference ?? ""
                    })
                    .ToList();

                // Get pending factures
                var pendingFacturesData = factures
                    .Where(f => f.Status != FactureStatus.Paid && f.Status != FactureStatus.Cancelled)
                    .OrderBy(f => f.DueDate)
                    .Take(10)
                    .Select(f => new FactureSummaryDto
                    {
                        TotalFactures = 1,
                        TotalOutstanding = f.OutstandingAmount,
                        TotalOutstandingDisplay = f.OutstandingAmountDisplay
                    })
                    .ToList();

                return new SupplierPaymentHistoryDto
                {
                    SupplierId = supplierId,
                    SupplierName = supplier.CompanyName,
                    SupplierCode = supplier.SupplierCode,
                    FromDate = startDate,
                    ToDate = endDate,
                    TotalFactures = totalFactures,
                    PaidFactures = paidFactures,
                    PendingFacturesCount = pendingFactures,
                    OverdueFactures = overdueFactures,
                    TotalInvoiced = totalInvoiced,
                    TotalPaid = totalPaid,
                    TotalOutstanding = totalOutstanding,
                    AveragePaymentDays = (decimal)averagePaymentDays,
                    LastPaymentDate = completedPayments.LastOrDefault()?.PaymentDate,
                    OldestUnpaidDate = factures.Where(f => f.OutstandingAmount > 0).Min(f => (DateTime?)f.InvoiceDate),
                    TotalInvoicedDisplay = totalInvoiced.ToString("C", new CultureInfo("id-ID")),
                    TotalPaidDisplay = totalPaid.ToString("C", new CultureInfo("id-ID")),
                    TotalOutstandingDisplay = totalOutstanding.ToString("C", new CultureInfo("id-ID")),
                    RecentPayments = recentPayments,
                    PendingFactures = pendingFacturesData
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving payment history for supplier {SupplierId}", supplierId);
                throw;
            }
        }

        public async Task<List<SupplierOutstandingDto>> GetSupplierOutstandingBalancesAsync(int? branchId = null, bool includeOverdueOnly = false)
        {
            try
            {
                var query = _context.Suppliers
                    .Include(s => s.Factures.Where(f => f.OutstandingAmount > 0))
                    .Where(s => s.IsActive)
                    .AsQueryable();

                if (branchId.HasValue)
                {
                    query = query.Where(s => s.BranchId == branchId.Value || s.BranchId == null);
                }

                var suppliers = await query.ToListAsync();

                var result = new List<SupplierOutstandingDto>();

                foreach (var supplier in suppliers)
                {
                    var outstandingFactures = supplier.Factures.Where(f => f.OutstandingAmount > 0).ToList();
                    var overdueFactures = outstandingFactures.Where(f => f.IsOverdue).ToList();

                    if (includeOverdueOnly && !overdueFactures.Any())
                        continue;

                    var totalOutstanding = outstandingFactures.Sum(f => f.OutstandingAmount);
                    var overdueAmount = overdueFactures.Sum(f => f.OutstandingAmount);
                    var creditUtilization = supplier.CreditLimit > 0 ? (totalOutstanding / supplier.CreditLimit) * 100 : 0;

                    var oldestUnpaid = outstandingFactures.Min(f => (DateTime?)f.InvoiceDate);
                    var daysOldestUnpaid = oldestUnpaid.HasValue ? (DateTime.UtcNow.Date - oldestUnpaid.Value.Date).Days : 0;

                    var riskLevel = "Low";
                    if (creditUtilization > 90 || daysOldestUnpaid > 90) riskLevel = "High";
                    else if (creditUtilization > 70 || daysOldestUnpaid > 60) riskLevel = "Medium";

                    result.Add(new SupplierOutstandingDto
                    {
                        SupplierId = supplier.Id,
                        SupplierName = supplier.CompanyName,
                        SupplierCode = supplier.SupplierCode,
                        OutstandingFactures = outstandingFactures.Count,
                        OverdueFactures = overdueFactures.Count,
                        TotalOutstanding = totalOutstanding,
                        OverdueAmount = overdueAmount,
                        CreditLimit = supplier.CreditLimit,
                        CreditUtilization = creditUtilization,
                        OldestUnpaidDate = oldestUnpaid,
                        DaysOldestUnpaid = daysOldestUnpaid,
                        TotalOutstandingDisplay = totalOutstanding.ToString("C", new CultureInfo("id-ID")),
                        OverdueAmountDisplay = overdueAmount.ToString("C", new CultureInfo("id-ID")),
                        CreditLimitDisplay = supplier.CreditLimit.ToString("C", new CultureInfo("id-ID")),
                        CreditUtilizationDisplay = $"{creditUtilization:F1}%",
                        IsCreditLimitExceeded = totalOutstanding > supplier.CreditLimit,
                        HasOverduePayments = overdueFactures.Any(),
                        RiskLevel = riskLevel
                    });
                }

                return result.OrderByDescending(r => r.TotalOutstanding).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving supplier outstanding balances for branch {BranchId}", branchId);
                throw;
            }
        }

        public async Task<SupplierCreditStatusDto> UpdateSupplierCreditStatusAsync(int supplierId)
        {
            try
            {
                var supplier = await _context.Suppliers
                    .Include(s => s.Factures.Where(f => f.InvoiceDate >= DateTime.UtcNow.AddMonths(-12)))
                    .ThenInclude(f => f.Payments)
                    .FirstOrDefaultAsync(s => s.Id == supplierId);

                if (supplier == null)
                    throw new InvalidOperationException($"Supplier with ID {supplierId} not found.");

                var factures = supplier.Factures.ToList();
                var currentOutstanding = factures.Sum(f => f.OutstandingAmount);
                var availableCredit = Math.Max(0, supplier.CreditLimit - currentOutstanding);
                var creditUtilization = supplier.CreditLimit > 0 ? (currentOutstanding / supplier.CreditLimit) * 100 : 0;

                // Calculate payment performance
                var completedPayments = factures
                    .Where(f => f.Status == FactureStatus.Paid && f.Payments.Any())
                    .SelectMany(f => f.Payments.Where(p => p.Status == PaymentStatus.Confirmed))
                    .ToList();

                var averagePaymentDays = completedPayments.Any()
                    ? (decimal)completedPayments.Average(p => (p.PaymentDate - p.Facture!.InvoiceDate).Days)
                    : 0;

                var paymentDelayIncidents = completedPayments.Count(p => (p.PaymentDate - p.Facture!.DueDate).Days > 0);
                var overdueFactures = factures.Where(f => f.IsOverdue).ToList();
                var totalOverdue = overdueFactures.Sum(f => f.OutstandingAmount);
                var daysOldestOverdue = overdueFactures.Any() 
                    ? overdueFactures.Max(f => f.DaysOverdue)
                    : 0;

                // Determine credit rating
                var creditRating = "Excellent";
                var riskLevel = "Low";
                var riskFactors = new List<string>();
                var recommendations = new List<string>();

                if (creditUtilization > 90)
                {
                    creditRating = "Poor";
                    riskLevel = "High";
                    riskFactors.Add("Credit utilization exceeds 90%");
                    recommendations.Add("Request payment for outstanding invoices");
                    recommendations.Add("Consider reducing credit limit");
                }
                else if (creditUtilization > 70)
                {
                    creditRating = averagePaymentDays > supplier.PaymentTerms + 7 ? "Poor" : "Fair";
                    riskLevel = "Medium";
                    riskFactors.Add("Credit utilization above 70%");
                    recommendations.Add("Monitor payment performance closely");
                }

                if (averagePaymentDays > supplier.PaymentTerms + 14)
                {
                    creditRating = "Poor";
                    riskLevel = "High";
                    riskFactors.Add($"Average payment delay: {averagePaymentDays - supplier.PaymentTerms:F0} days");
                    recommendations.Add("Review payment terms with supplier");
                }

                if (overdueFactures.Any())
                {
                    riskFactors.Add($"{overdueFactures.Count} overdue invoices");
                    recommendations.Add("Follow up on overdue payments immediately");
                }

                if (paymentDelayIncidents > factures.Count * 0.3)
                {
                    riskFactors.Add("Frequent payment delays");
                    recommendations.Add("Consider stricter payment terms");
                }

                return new SupplierCreditStatusDto
                {
                    SupplierId = supplierId,
                    SupplierName = supplier.CompanyName,
                    CreditLimit = supplier.CreditLimit,
                    CurrentOutstanding = currentOutstanding,
                    AvailableCredit = availableCredit,
                    CreditUtilization = creditUtilization,
                    CreditRating = creditRating,
                    RiskLevel = riskLevel,
                    AveragePaymentDays = averagePaymentDays,
                    PaymentDelayIncidents = paymentDelayIncidents,
                    LastPaymentDate = completedPayments.LastOrDefault()?.PaymentDate,
                    HasOverduePayments = overdueFactures.Any(),
                    TotalOverdue = totalOverdue,
                    DaysOldestOverdue = daysOldestOverdue,
                    AssessmentDate = _timezoneService.UtcToLocal(DateTime.UtcNow),
                    CreditLimitDisplay = supplier.CreditLimit.ToString("C", new CultureInfo("id-ID")),
                    CurrentOutstandingDisplay = currentOutstanding.ToString("C", new CultureInfo("id-ID")),
                    AvailableCreditsDisplay = availableCredit.ToString("C", new CultureInfo("id-ID")),
                    CreditUtilizationDisplay = $"{creditUtilization:F1}%",
                    TotalOverdueDisplay = totalOverdue.ToString("C", new CultureInfo("id-ID")),
                    RiskFactors = riskFactors,
                    Recommendations = recommendations
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating credit status for supplier {SupplierId}", supplierId);
                throw;
            }
        }

        public async Task<SupplierPaymentAnalyticsDto> GetSupplierPaymentAnalyticsAsync(int supplierId, int monthsBack = 12)
        {
            try
            {
                var supplier = await _context.Suppliers.FindAsync(supplierId);
                if (supplier == null)
                    throw new InvalidOperationException($"Supplier with ID {supplierId} not found.");

                var analysisFromDate = DateTime.UtcNow.AddMonths(-monthsBack);
                var analysisToDate = DateTime.UtcNow;

                var factures = await _context.Factures
                    .Include(f => f.Payments)
                    .Where(f => f.SupplierId == supplierId && 
                               f.InvoiceDate >= analysisFromDate && 
                               f.InvoiceDate <= analysisToDate)
                    .ToListAsync();

                var totalFactures = factures.Count;
                var onTimePayments = 0;
                var latePayments = 0;
                var paymentDays = new List<double>();

                foreach (var facture in factures.Where(f => f.Status == FactureStatus.Paid))
                {
                    var firstPayment = facture.Payments
                        .Where(p => p.Status == PaymentStatus.Confirmed)
                        .OrderBy(p => p.PaymentDate)
                        .FirstOrDefault();

                    if (firstPayment != null)
                    {
                        var daysToPay = (firstPayment.PaymentDate - facture.InvoiceDate).Days;
                        paymentDays.Add(daysToPay);

                        if (daysToPay <= supplier.PaymentTerms)
                            onTimePayments++;
                        else
                            latePayments++;
                    }
                }

                var onTimePaymentRate = totalFactures > 0 ? (decimal)onTimePayments / totalFactures * 100 : 0;
                var averagePaymentDays = paymentDays.Any() ? (decimal)paymentDays.Average() : 0;
                var medianPaymentDays = paymentDays.Any() ? (decimal)paymentDays.OrderBy(d => d).Skip(paymentDays.Count / 2).First() : 0;

                var totalVolumeInvoiced = factures.Sum(f => f.TotalAmount);
                var totalVolumePaid = factures.Sum(f => f.PaidAmount);
                var largestInvoice = factures.Any() ? factures.Max(f => f.TotalAmount) : 0;
                var smallestInvoice = factures.Any() ? factures.Min(f => f.TotalAmount) : 0;
                var averageInvoiceAmount = factures.Any() ? factures.Average(f => f.TotalAmount) : 0;

                // Calculate payment compliance score (0-100)
                var paymentComplianceScore = (onTimePaymentRate * 0.4m) + 
                                           ((averagePaymentDays <= supplier.PaymentTerms ? 100 : Math.Max(0, 100 - ((averagePaymentDays - supplier.PaymentTerms) * 2))) * 0.6m);

                // Determine payment trend
                var paymentTrend = "Stable";
                var recentMonths = factures.Where(f => f.InvoiceDate >= DateTime.UtcNow.AddMonths(-3)).ToList();
                var olderMonths = factures.Where(f => f.InvoiceDate < DateTime.UtcNow.AddMonths(-3) && f.InvoiceDate >= DateTime.UtcNow.AddMonths(-6)).ToList();

                if (recentMonths.Any() && olderMonths.Any())
                {
                    var recentAvg = recentMonths.Where(f => f.Status == FactureStatus.Paid)
                        .SelectMany(f => f.Payments.Where(p => p.Status == PaymentStatus.Confirmed))
                        .Average(p => (p.PaymentDate - p.Facture!.InvoiceDate).Days);
                    
                    var olderAvg = olderMonths.Where(f => f.Status == FactureStatus.Paid)
                        .SelectMany(f => f.Payments.Where(p => p.Status == PaymentStatus.Confirmed))
                        .Average(p => (p.PaymentDate - p.Facture!.InvoiceDate).Days);

                    if (recentAvg < olderAvg - 2) paymentTrend = "Improving";
                    else if (recentAvg > olderAvg + 2) paymentTrend = "Declining";
                }

                // Generate monthly performance
                var monthlyPerformance = new List<MonthlyPaymentPerformanceDto>();
                for (int i = monthsBack - 1; i >= 0; i--)
                {
                    var monthStart = DateTime.UtcNow.AddMonths(-i).Date.AddDays(1 - DateTime.UtcNow.AddMonths(-i).Day);
                    var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                    
                    var monthFactures = factures.Where(f => f.InvoiceDate >= monthStart && f.InvoiceDate <= monthEnd).ToList();
                    var monthOnTime = 0;
                    var monthLate = 0;
                    var monthPaymentDays = new List<double>();

                    foreach (var facture in monthFactures.Where(f => f.Status == FactureStatus.Paid))
                    {
                        var payment = facture.Payments.Where(p => p.Status == PaymentStatus.Confirmed)
                            .OrderBy(p => p.PaymentDate).FirstOrDefault();
                        if (payment != null)
                        {
                            var days = (payment.PaymentDate - facture.InvoiceDate).Days;
                            monthPaymentDays.Add(days);
                            if (days <= supplier.PaymentTerms) monthOnTime++; else monthLate++;
                        }
                    }

                    monthlyPerformance.Add(new MonthlyPaymentPerformanceDto
                    {
                        Year = monthStart.Year,
                        Month = monthStart.Month,
                        MonthName = monthStart.ToString("MMMM yyyy", new CultureInfo("id-ID")),
                        TotalFactures = monthFactures.Count,
                        OnTimePayments = monthOnTime,
                        LatePayments = monthLate,
                        OnTimeRate = monthFactures.Count > 0 ? (decimal)monthOnTime / monthFactures.Count * 100 : 0,
                        AveragePaymentDays = monthPaymentDays.Any() ? (decimal)monthPaymentDays.Average() : 0,
                        TotalInvoiced = monthFactures.Sum(f => f.TotalAmount),
                        TotalPaid = monthFactures.Sum(f => f.PaidAmount),
                        TotalInvoicedDisplay = monthFactures.Sum(f => f.TotalAmount).ToString("C", new CultureInfo("id-ID")),
                        TotalPaidDisplay = monthFactures.Sum(f => f.PaidAmount).ToString("C", new CultureInfo("id-ID"))
                    });
                }

                return new SupplierPaymentAnalyticsDto
                {
                    SupplierId = supplierId,
                    SupplierName = supplier.CompanyName,
                    AnalysisFromDate = analysisFromDate,
                    AnalysisToDate = analysisToDate,
                    TotalFactures = totalFactures,
                    OnTimePayments = onTimePayments,
                    LatePayments = latePayments,
                    OnTimePaymentRate = onTimePaymentRate,
                    AveragePaymentDays = averagePaymentDays,
                    MedianPaymentDays = medianPaymentDays,
                    TotalVolumeInvoiced = totalVolumeInvoiced,
                    TotalVolumePaid = totalVolumePaid,
                    LargestInvoice = largestInvoice,
                    SmallestInvoice = smallestInvoice,
                    AverageInvoiceAmount = averageInvoiceAmount,
                    PaymentTermDays = supplier.PaymentTerms,
                    PaymentComplianceScore = paymentComplianceScore,
                    PaymentTrend = paymentTrend,
                    TotalVolumeInvoicedDisplay = totalVolumeInvoiced.ToString("C", new CultureInfo("id-ID")),
                    TotalVolumePaidDisplay = totalVolumePaid.ToString("C", new CultureInfo("id-ID")),
                    LargestInvoiceDisplay = largestInvoice.ToString("C", new CultureInfo("id-ID")),
                    AverageInvoiceAmountDisplay = averageInvoiceAmount.ToString("C", new CultureInfo("id-ID")),
                    MonthlyPerformance = monthlyPerformance
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving payment analytics for supplier {SupplierId}", supplierId);
                throw;
            }
        }

        public async Task<List<SupplierCreditWarningDto>> GetSuppliersWithCreditWarningsAsync(int? branchId = null, decimal warningThreshold = 80)
        {
            try
            {
                var query = _context.Suppliers
                    .Include(s => s.Factures.Where(f => f.OutstandingAmount > 0))
                    .Where(s => s.IsActive && s.CreditLimit > 0)
                    .AsQueryable();

                if (branchId.HasValue)
                {
                    query = query.Where(s => s.BranchId == branchId.Value || s.BranchId == null);
                }

                var suppliers = await query.ToListAsync();
                var warnings = new List<SupplierCreditWarningDto>();

                foreach (var supplier in suppliers)
                {
                    var currentOutstanding = supplier.Factures.Sum(f => f.OutstandingAmount);
                    var creditUtilization = (currentOutstanding / supplier.CreditLimit) * 100;

                    if (creditUtilization >= warningThreshold)
                    {
                        var warningLevel = "Warning";
                        var excessAmount = 0m;
                        var requiresImmediateAction = false;
                        var recommendedActions = new List<string>();

                        if (creditUtilization >= 100)
                        {
                            warningLevel = "Exceeded";
                            excessAmount = currentOutstanding - supplier.CreditLimit;
                            requiresImmediateAction = true;
                            recommendedActions.Add("Stop accepting new orders");
                            recommendedActions.Add("Request immediate payment");
                            recommendedActions.Add("Consider legal action if needed");
                        }
                        else if (creditUtilization >= 95)
                        {
                            warningLevel = "Critical";
                            requiresImmediateAction = true;
                            recommendedActions.Add("Contact supplier immediately");
                            recommendedActions.Add("Request payment plan");
                            recommendedActions.Add("Hold new orders pending payment");
                        }
                        else
                        {
                            recommendedActions.Add("Monitor closely");
                            recommendedActions.Add("Follow up on outstanding invoices");
                        }

                        warnings.Add(new SupplierCreditWarningDto
                        {
                            SupplierId = supplier.Id,
                            SupplierName = supplier.CompanyName,
                            SupplierCode = supplier.SupplierCode,
                            CreditLimit = supplier.CreditLimit,
                            CurrentOutstanding = currentOutstanding,
                            CreditUtilization = creditUtilization,
                            WarningThreshold = warningThreshold,
                            ExcessAmount = excessAmount,
                            WarningLevel = warningLevel,
                            LastUpdated = _timezoneService.UtcToLocal(DateTime.UtcNow),
                            CreditLimitDisplay = supplier.CreditLimit.ToString("C", new CultureInfo("id-ID")),
                            CurrentOutstandingDisplay = currentOutstanding.ToString("C", new CultureInfo("id-ID")),
                            CreditUtilizationDisplay = $"{creditUtilization:F1}%",
                            ExcessAmountDisplay = excessAmount.ToString("C", new CultureInfo("id-ID")),
                            RequiresImmediateAction = requiresImmediateAction,
                            RecommendedActions = recommendedActions
                        });
                    }
                }

                return warnings.OrderByDescending(w => w.CreditUtilization).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving credit warnings for branch {BranchId}", branchId);
                throw;
            }
        }

        public async Task<SupplierPaymentScheduleDto> GetSupplierPaymentScheduleAsync(int supplierId, int daysAhead = 30)
        {
            try
            {
                var supplier = await _context.Suppliers.FindAsync(supplierId);
                if (supplier == null)
                    throw new InvalidOperationException($"Supplier with ID {supplierId} not found.");

                var scheduleToDate = DateTime.UtcNow.AddDays(daysAhead);
                
                var factures = await _context.Factures
                    .Include(f => f.Payments)
                    .Where(f => f.SupplierId == supplierId && 
                               f.OutstandingAmount > 0 &&
                               (f.DueDate <= scheduleToDate || f.IsOverdue))
                    .OrderBy(f => f.DueDate)
                    .ToListAsync();

                var upcomingPayments = factures
                    .Where(f => !f.IsOverdue)
                    .Select(f => new UpcomingPaymentDto
                    {
                        FactureId = f.Id,
                        InternalReferenceNumber = f.InternalReferenceNumber,
                        SupplierInvoiceNumber = f.SupplierInvoiceNumber,
                        DueDate = f.DueDate,
                        Amount = f.OutstandingAmount,
                        DaysUntilDue = f.DaysUntilDue,
                        Priority = f.PriorityDisplay,
                        AmountDisplay = f.OutstandingAmountDisplay,
                        IsDueToday = f.DaysUntilDue == 0,
                        IsDueSoon = f.DaysUntilDue <= 7,
                        HasScheduledPayment = f.Payments.Any(p => p.Status == PaymentStatus.Scheduled),
                        ScheduledPaymentDate = f.Payments.Where(p => p.Status == PaymentStatus.Scheduled)
                                                        .OrderBy(p => p.PaymentDate)
                                                        .FirstOrDefault()?.PaymentDate
                    })
                    .ToList();

                var overduePayments = factures
                    .Where(f => f.IsOverdue)
                    .Select(f => new OverduePaymentDto
                    {
                        FactureId = f.Id,
                        InternalReferenceNumber = f.InternalReferenceNumber,
                        SupplierInvoiceNumber = f.SupplierInvoiceNumber,
                        DueDate = f.DueDate,
                        Amount = f.OutstandingAmount,
                        DaysOverdue = f.DaysOverdue,
                        AmountDisplay = f.OutstandingAmountDisplay,
                        UrgencyLevel = f.DaysOverdue > 30 ? "Critical" : f.DaysOverdue > 7 ? "High" : "Medium",
                        RequiresImmediateAction = f.DaysOverdue > 7
                    })
                    .ToList();

                var totalUpcoming = upcomingPayments.Sum(p => p.Amount);
                var totalOverdue = overduePayments.Sum(p => p.Amount);
                var amountDueToday = upcomingPayments.Where(p => p.IsDueToday).Sum(p => p.Amount);
                var amountDueThisWeek = upcomingPayments.Where(p => p.DaysUntilDue <= 7).Sum(p => p.Amount);

                return new SupplierPaymentScheduleDto
                {
                    SupplierId = supplierId,
                    SupplierName = supplier.CompanyName,
                    ScheduleFromDate = DateTime.UtcNow.Date,
                    ScheduleToDate = scheduleToDate,
                    TotalUpcomingPayments = upcomingPayments.Count,
                    TotalUpcomingAmount = totalUpcoming,
                    OverduePaymentsCount = overduePayments.Count,
                    OverdueAmount = totalOverdue,
                    PaymentsDueToday = upcomingPayments.Count(p => p.IsDueToday),
                    AmountDueToday = amountDueToday,
                    PaymentsDueThisWeek = upcomingPayments.Count(p => p.DaysUntilDue <= 7),
                    AmountDueThisWeek = amountDueThisWeek,
                    TotalUpcomingAmountDisplay = totalUpcoming.ToString("C", new CultureInfo("id-ID")),
                    OverdueAmountDisplay = totalOverdue.ToString("C", new CultureInfo("id-ID")),
                    AmountDueTodayDisplay = amountDueToday.ToString("C", new CultureInfo("id-ID")),
                    AmountDueThisWeekDisplay = amountDueThisWeek.ToString("C", new CultureInfo("id-ID")),
                    UpcomingPayments = upcomingPayments,
                    OverduePayments = overduePayments
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving payment schedule for supplier {SupplierId}", supplierId);
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