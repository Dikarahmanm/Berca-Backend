using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using Berca_Backend.DTOs;
using Berca_Backend.Services;
using Berca_Backend.Services.Interfaces;
using Berca_Backend.Models;
using Berca_Backend.Data;

namespace Berca_Backend.Controllers
{
    /// <summary>
    /// Supplier Controller for multi-branch supplier management
    /// Handles supplier CRUD operations with branch integration and authorization
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SupplierController : ControllerBase
    {
        private readonly ISupplierService _supplierService;
        private readonly IMemoryCache _cache;
        private readonly ICacheInvalidationService _cacheInvalidation;
        private readonly ILogger<SupplierController> _logger;
        private readonly AppDbContext _context;

        public SupplierController(
            ISupplierService supplierService,
            IMemoryCache cache,
            ICacheInvalidationService cacheInvalidation,
            ILogger<SupplierController> logger,
            AppDbContext context)
        {
            _supplierService = supplierService;
            _cache = cache;
            _cacheInvalidation = cacheInvalidation;
            _logger = logger;
            _context = context;
        }

        // ==================== CRUD ENDPOINTS ==================== //

        /// <summary>
        /// Get suppliers with filtering, searching and pagination (Required by frontend integration)
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "Supplier.Read")]
        public async Task<IActionResult> GetSuppliers(
            [FromQuery] string? search = null,
            [FromQuery] string? branchIds = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string sortBy = "Name",
            [FromQuery] string sortOrder = "asc")
        {
            try
            {
                // ✅ CACHE ASIDE PATTERN: Check cache first
                var cacheKey = $"suppliers_{search ?? "all"}_{branchIds ?? "all"}_{isActive}_{page}_{pageSize}_{sortBy}_{sortOrder}_{GetCurrentUserId()}";

                if (_cache.TryGetValue(cacheKey, out object? cachedSuppliers))
                {
                    _logger.LogInformation("🔄 Cache HIT: Retrieved suppliers from cache");
                    return Ok(cachedSuppliers);
                }

                _logger.LogInformation("🔄 Cache MISS: Fetching suppliers from database");
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();
                
                if (!currentUserId.HasValue || currentUserId.Value == 0)
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Invalid user session"
                    });
                }

                // Parse and validate branch IDs
                var requestedBranchIds = new List<int>();
                if (!string.IsNullOrEmpty(branchIds))
                {
                    requestedBranchIds = branchIds.Split(',')
                        .Where(id => int.TryParse(id.Trim(), out _))
                        .Select(id => int.Parse(id.Trim()))
                        .ToList();
                }

                // Get user's accessible branches
                var accessibleBranchIds = await GetUserAccessibleBranches(currentUserId.Value, currentUserRole);
                
                // Filter requested branches by user access
                if (requestedBranchIds.Any())
                {
                    requestedBranchIds = requestedBranchIds.Intersect(accessibleBranchIds).ToList();
                }
                else
                {
                    requestedBranchIds = accessibleBranchIds;
                }

                if (!requestedBranchIds.Any())
                {
                    return StatusCode(403, new
                    {
                        success = false,
                        message = "No accessible branches found"
                    });
                }

                // Build suppliers query with branch filtering
                var suppliersQuery = _context.Suppliers
                    .Include(s => s.Branches)
                    .Where(s => s.Branches.Any(sb => requestedBranchIds.Contains(sb.Id)) || 
                               !s.Branches.Any()); // Include global suppliers

                // Apply search filter
                if (!string.IsNullOrEmpty(search))
                {
                    suppliersQuery = suppliersQuery.Where(s => 
                        s.Name.Contains(search) ||
                        s.CompanyName.Contains(search) ||
                        s.SupplierCode.Contains(search) ||
                        s.Email.Contains(search));
                }

                // Apply active filter
                if (isActive.HasValue)
                {
                    suppliersQuery = suppliersQuery.Where(s => s.IsActive == isActive.Value);
                }

                // Apply sorting
                suppliersQuery = sortBy.ToLower() switch
                {
                    "name" => sortOrder.ToLower() == "desc" ? 
                        suppliersQuery.OrderByDescending(s => s.Name) : 
                        suppliersQuery.OrderBy(s => s.Name),
                    "company" => sortOrder.ToLower() == "desc" ? 
                        suppliersQuery.OrderByDescending(s => s.CompanyName) : 
                        suppliersQuery.OrderBy(s => s.CompanyName),
                    "created" => sortOrder.ToLower() == "desc" ? 
                        suppliersQuery.OrderByDescending(s => s.CreatedAt) : 
                        suppliersQuery.OrderBy(s => s.CreatedAt),
                    _ => suppliersQuery.OrderBy(s => s.Name)
                };

                // Get total count
                var totalCount = await suppliersQuery.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                // Apply pagination
                var suppliers = await suppliersQuery
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(s => new
                    {
                        id = s.Id,
                        supplierCode = s.SupplierCode,
                        name = s.Name,
                        companyName = s.CompanyName,
                        email = s.Email,
                        phone = s.Phone,
                        address = s.Address,
                        paymentTerms = s.PaymentTerms,
                        creditLimit = s.CreditLimit,
                        currentBalance = s.CurrentBalance,
                        isActive = s.IsActive,
                        branches = s.Branches.Select(b => new
                        {
                            id = b.Id,
                            name = b.BranchName
                        }).ToList(),
                        createdAt = s.CreatedAt,
                        updatedAt = s.UpdatedAt
                    })
                    .ToListAsync();

                // Prepare the response object
                var suppliersResponse = new
                {
                    success = true,
                    data = suppliers,
                    pagination = new
                    {
                        currentPage = page,
                        totalPages = totalPages,
                        totalItems = totalCount,
                        itemsPerPage = pageSize
                    }
                };

                // ✅ CACHE ASIDE PATTERN: Update cache after database fetch
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30), // Suppliers cache for 30 minutes
                    SlidingExpiration = TimeSpan.FromMinutes(10),
                    Priority = CacheItemPriority.Normal
                };

                _cache.Set(cacheKey, suppliersResponse, cacheOptions);
                _logger.LogInformation("💾 Cache UPDATED: Stored suppliers in cache");

                return Ok(suppliersResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving suppliers");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get supplier by ID (Required by frontend integration)
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Policy = "Supplier.Read")]
        public async Task<IActionResult> GetSupplierById(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();
                
                if (!currentUserId.HasValue || currentUserId.Value == 0)
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Invalid user session"
                    });
                }

                // Get user's accessible branches
                var accessibleBranchIds = await GetUserAccessibleBranches(currentUserId.Value, currentUserRole);

                var supplier = await _context.Suppliers
                    .Include(s => s.Branches)
                    .Where(s => s.Id == id)
                    .Where(s => s.Branches.Any(sb => accessibleBranchIds.Contains(sb.Id)) || 
                               !s.Branches.Any()) // Include global suppliers
                    .Select(s => new
                    {
                        id = s.Id,
                        supplierCode = s.SupplierCode,
                        name = s.Name,
                        companyName = s.CompanyName,
                        email = s.Email,
                        phone = s.Phone,
                        address = s.Address,
                        city = s.City,
                        province = s.Province,
                        postalCode = s.PostalCode,
                        country = s.Country,
                        contactPerson = s.ContactPerson,
                        contactPhone = s.ContactPhone,
                        contactEmail = s.ContactEmail,
                        paymentTerms = s.PaymentTerms,
                        creditLimit = s.CreditLimit,
                        currentBalance = s.CurrentBalance,
                        taxNumber = s.TaxNumber,
                        bankAccount = s.BankAccount,
                        bankName = s.BankName,
                        isActive = s.IsActive,
                        notes = s.Notes,
                        branches = s.Branches.Select(b => new
                        {
                            id = b.Id,
                            name = b.BranchName,
                            type = b.BranchType.ToString()
                        }).ToList(),
                        createdAt = s.CreatedAt,
                        updatedAt = s.UpdatedAt
                    })
                    .FirstOrDefaultAsync();

                if (supplier == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Supplier not found or not accessible"
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = supplier
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving supplier {SupplierId}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get supplier by supplier code
        /// </summary>
        /// <param name="code">Supplier code</param>
        /// <returns>Supplier details</returns>
        [HttpGet("code/{code}")]
        [Authorize(Policy = "Supplier.Read")]
        public async Task<IActionResult> GetSupplierByCode(string code)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                    return Unauthorized(ApiResponse<SupplierDto>.ErrorResponse("User not authenticated"));

                var supplier = await _supplierService.GetSupplierByCodeAsync(code, currentUserId.Value);
                if (supplier == null)
                    return NotFound(ApiResponse<SupplierDto>.ErrorResponse("Supplier not found"));

                return Ok(ApiResponse<SupplierDto>.SuccessResponse(supplier, "Supplier retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving supplier by code {SupplierCode}", code);
                return StatusCode(500, ApiResponse<SupplierDto>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Create new supplier
        /// </summary>
        /// <param name="createDto">Supplier creation data</param>
        /// <returns>Created supplier</returns>
        [HttpPost]
        [Authorize(Policy = "Supplier.Write")]
        public async Task<IActionResult> CreateSupplier([FromBody] CreateSupplierDto createDto)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                    return Unauthorized(ApiResponse<SupplierDto>.ErrorResponse("User not authenticated"));

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return BadRequest(ApiResponse<SupplierDto>.ErrorResponse("Validation failed", errors));
                }

                var supplier = await _supplierService.CreateSupplierAsync(createDto, currentUserId.Value);

                // ✅ CACHE INVALIDATION: Clear supplier-related caches after creation
                _cacheInvalidation.InvalidateByPattern("suppliers_*");

                _logger.LogInformation("🗑️ Cache invalidated after supplier creation: (ID: {SupplierId})",
                    supplier.Id);

                return CreatedAtAction(
                    nameof(GetSupplierById),
                    new { id = supplier.Id },
                    ApiResponse<SupplierDto>.SuccessResponse(supplier, "Supplier created successfully"));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Business rule violation creating supplier");
                return BadRequest(ApiResponse<SupplierDto>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating supplier");
                return StatusCode(500, ApiResponse<SupplierDto>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Update existing supplier
        /// </summary>
        /// <param name="id">Supplier ID</param>
        /// <param name="updateDto">Supplier update data</param>
        /// <returns>Updated supplier</returns>
        [HttpPut("{id}")]
        [Authorize(Policy = "Supplier.Write")]
        public async Task<IActionResult> UpdateSupplier(int id, [FromBody] UpdateSupplierDto updateDto)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                    return Unauthorized(ApiResponse<SupplierDto>.ErrorResponse("User not authenticated"));

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return BadRequest(ApiResponse<SupplierDto>.ErrorResponse("Validation failed", errors));
                }

                var supplier = await _supplierService.UpdateSupplierAsync(id, updateDto, currentUserId.Value);
                if (supplier == null)
                    return NotFound(ApiResponse<SupplierDto>.ErrorResponse("Supplier not found"));

                // ✅ CACHE INVALIDATION: Clear supplier-related caches after update
                _cacheInvalidation.InvalidateByPattern("suppliers_*");

                _logger.LogInformation("🗑️ Cache invalidated after supplier update: {SupplierId}", id);

                return Ok(ApiResponse<SupplierDto>.SuccessResponse(supplier, "Supplier updated successfully"));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Business rule violation updating supplier {SupplierId}", id);
                return BadRequest(ApiResponse<SupplierDto>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating supplier {SupplierId}", id);
                return StatusCode(500, ApiResponse<SupplierDto>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Delete supplier (soft delete)
        /// </summary>
        /// <param name="id">Supplier ID</param>
        /// <param name="reason">Reason for deletion</param>
        /// <returns>Success status</returns>
        [HttpDelete("{id}")]
        [Authorize(Policy = "Supplier.Delete")]
        public async Task<IActionResult> DeleteSupplier(int id, [FromQuery] string? reason = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                    return Unauthorized(ApiResponse<object>.ErrorResponse("User not authenticated"));

                var result = await _supplierService.DeleteSupplierAsync(id, currentUserId.Value, reason);
                if (!result)
                    return NotFound(ApiResponse<object>.ErrorResponse("Supplier not found"));

                return Ok(ApiResponse<object>.SuccessResponse(new object(), "Supplier deleted successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting supplier {SupplierId}", id);
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Toggle supplier status (active/inactive)
        /// </summary>
        /// <param name="id">Supplier ID</param>
        /// <param name="statusDto">Status change data</param>
        /// <returns>Updated supplier</returns>
        [HttpPatch("{id}/status")]
        [Authorize(Policy = "Supplier.Write")]
        public async Task<IActionResult> ToggleSupplierStatus(int id, [FromBody] SupplierStatusDto statusDto)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                    return Unauthorized(ApiResponse<SupplierDto>.ErrorResponse("User not authenticated"));

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return BadRequest(ApiResponse<SupplierDto>.ErrorResponse("Validation failed", errors));
                }

                var supplier = await _supplierService.ToggleSupplierStatusAsync(
                    id, statusDto.IsActive, currentUserId.Value, statusDto.Reason);

                if (supplier == null)
                    return NotFound(ApiResponse<SupplierDto>.ErrorResponse("Supplier not found"));

                var message = $"Supplier status changed to {(statusDto.IsActive ? "active" : "inactive")}";
                return Ok(ApiResponse<SupplierDto>.SuccessResponse(supplier, message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling supplier {SupplierId} status", id);
                return StatusCode(500, ApiResponse<SupplierDto>.ErrorResponse("Internal server error"));
            }
        }

        // ==================== BRANCH-SPECIFIC ENDPOINTS ==================== //

        /// <summary>
        /// Get suppliers available to specific branch
        /// </summary>
        /// <param name="branchId">Branch ID</param>
        /// <param name="includeAll">Include suppliers available to all branches</param>
        /// <param name="activeOnly">Return only active suppliers</param>
        /// <returns>List of suppliers for the branch</returns>
        [HttpGet("branch/{branchId}")]
        [Authorize(Policy = "Supplier.Read")]
        public async Task<IActionResult> GetSuppliersByBranch(
            int branchId,
            [FromQuery] bool includeAll = true,
            [FromQuery] bool activeOnly = true)
        {
            try
            {
                var suppliers = await _supplierService.GetSuppliersByBranchAsync(branchId, includeAll, activeOnly);
                return Ok(ApiResponse<List<SupplierSummaryDto>>.SuccessResponse(
                    suppliers, $"Retrieved {suppliers.Count} suppliers for branch"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving suppliers for branch {BranchId}", branchId);
                return StatusCode(500, ApiResponse<List<SupplierSummaryDto>>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get suppliers by payment terms range
        /// </summary>
        /// <param name="minDays">Minimum payment terms (days)</param>
        /// <param name="maxDays">Maximum payment terms (days)</param>
        /// <param name="branchId">Optional branch filter</param>
        /// <returns>List of suppliers matching payment terms</returns>
        [HttpGet("payment-terms")]
        [Authorize(Policy = "Supplier.Read")]
        public async Task<IActionResult> GetSuppliersByPaymentTerms(
            [FromQuery, Range(1, 365)] int minDays = 1,
            [FromQuery, Range(1, 365)] int maxDays = 365,
            [FromQuery] int? branchId = null)
        {
            try
            {
                if (minDays > maxDays)
                    return BadRequest(ApiResponse<List<SupplierSummaryDto>>.ErrorResponse("Minimum days cannot be greater than maximum days"));

                var suppliers = await _supplierService.GetActiveSuppliersByPaymentTermsAsync(minDays, maxDays, branchId);
                return Ok(ApiResponse<List<SupplierSummaryDto>>.SuccessResponse(
                    suppliers, $"Retrieved {suppliers.Count} suppliers with {minDays}-{maxDays} day payment terms"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving suppliers by payment terms {MinDays}-{MaxDays}", minDays, maxDays);
                return StatusCode(500, ApiResponse<List<SupplierSummaryDto>>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get suppliers by minimum credit limit
        /// </summary>
        /// <param name="minCreditLimit">Minimum credit limit</param>
        /// <param name="branchId">Optional branch filter</param>
        /// <returns>List of suppliers with sufficient credit limit</returns>
        [HttpGet("credit-limit")]
        [Authorize(Policy = "Supplier.Read")]
        public async Task<IActionResult> GetSuppliersByCreditLimit(
            [FromQuery, Range(0, 999999999)] decimal minCreditLimit,
            [FromQuery] int? branchId = null)
        {
            try
            {
                var suppliers = await _supplierService.GetSuppliersByCreditLimitAsync(minCreditLimit, branchId);
                return Ok(ApiResponse<List<SupplierSummaryDto>>.SuccessResponse(
                    suppliers, $"Retrieved {suppliers.Count} suppliers with credit limit >= {minCreditLimit:C}"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving suppliers by credit limit {MinCreditLimit}", minCreditLimit);
                return StatusCode(500, ApiResponse<List<SupplierSummaryDto>>.ErrorResponse("Internal server error"));
            }
        }

        // ==================== VALIDATION ENDPOINTS ==================== //

        /// <summary>
        /// Check if supplier code is unique
        /// </summary>
        /// <param name="code">Supplier code to check</param>
        /// <param name="excludeId">Supplier ID to exclude from check</param>
        /// <returns>True if unique</returns>
        [HttpGet("validate/code/{code}")]
        [Authorize(Policy = "Supplier.Read")]
        public async Task<IActionResult> ValidateSupplierCode(string code, [FromQuery] int? excludeId = null)
        {
            try
            {
                var isUnique = await _supplierService.IsSupplierCodeUniqueAsync(code, excludeId);
                return Ok(ApiResponse<bool>.SuccessResponse(isUnique, 
                    isUnique ? "Supplier code is available" : "Supplier code is already in use"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating supplier code {Code}", code);
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Check if company name is unique within branch
        /// </summary>
        /// <param name="name">Company name to check</param>
        /// <param name="branchId">Branch ID</param>
        /// <param name="excludeId">Supplier ID to exclude from check</param>
        /// <returns>True if unique</returns>
        [HttpGet("validate/company")]
        [Authorize(Policy = "Supplier.Read")]
        public async Task<IActionResult> ValidateCompanyName(
            [FromQuery] string name,
            [FromQuery] int? branchId = null,
            [FromQuery] int? excludeId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return BadRequest(ApiResponse<bool>.ErrorResponse("Company name is required"));

                var isUnique = await _supplierService.IsCompanyNameUniqueAsync(name, branchId, excludeId);
                return Ok(ApiResponse<bool>.SuccessResponse(isUnique,
                    isUnique ? "Company name is available" : "Company name is already in use in this branch"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating company name {Name} for branch {BranchId}", name, branchId);
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Check if email is unique
        /// </summary>
        /// <param name="email">Email to check</param>
        /// <param name="excludeId">Supplier ID to exclude from check</param>
        /// <returns>True if unique</returns>
        [HttpGet("validate/email")]
        [Authorize(Policy = "Supplier.Read")]
        public async Task<IActionResult> ValidateEmail(
            [FromQuery] string email,
            [FromQuery] int? excludeId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return BadRequest(ApiResponse<bool>.ErrorResponse("Email is required"));

                var isUnique = await _supplierService.IsEmailUniqueAsync(email, excludeId);
                return Ok(ApiResponse<bool>.SuccessResponse(isUnique,
                    isUnique ? "Email is available" : "Email is already in use"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating email {Email}", email);
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Internal server error"));
            }
        }

        // ==================== ANALYTICS & REPORTING ENDPOINTS ==================== //

        /// <summary>
        /// Get supplier statistics for dashboard
        /// </summary>
        /// <param name="branchId">Optional branch filter</param>
        /// <returns>Supplier statistics</returns>
        [HttpGet("stats")]
        [Authorize(Policy = "Reports.Read")]
        public async Task<IActionResult> GetSupplierStats([FromQuery] int? branchId = null)
        {
            try
            {
                var stats = await _supplierService.GetSupplierStatsAsync(branchId);
                return Ok(ApiResponse<SupplierStatsDto>.SuccessResponse(stats, "Supplier statistics retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving supplier stats for branch {BranchId}", branchId);
                return StatusCode(500, ApiResponse<SupplierStatsDto>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get suppliers requiring attention
        /// </summary>
        /// <param name="branchId">Optional branch filter</param>
        /// <returns>List of supplier alerts</returns>
        [HttpGet("alerts")]
        [Authorize(Policy = "Reports.Read")]
        public async Task<IActionResult> GetSupplierAlerts([FromQuery] int? branchId = null)
        {
            try
            {
                var alerts = await _supplierService.GetSuppliersRequiringAttentionAsync(branchId);
                return Ok(ApiResponse<List<SupplierAlertDto>>.SuccessResponse(
                    alerts, $"Retrieved {alerts.Count} supplier alerts"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving supplier alerts for branch {BranchId}", branchId);
                return StatusCode(500, ApiResponse<List<SupplierAlertDto>>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Generate new supplier code
        /// </summary>
        /// <returns>Generated supplier code</returns>
        [HttpGet("generate-code")]
        [Authorize(Policy = "Supplier.Write")]
        public async Task<IActionResult> GenerateSupplierCode()
        {
            try
            {
                var code = await _supplierService.GenerateSupplierCodeAsync();
                return Ok(ApiResponse<string>.SuccessResponse(code, "Supplier code generated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating supplier code");
                return StatusCode(500, ApiResponse<string>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Assign supplier to branches (Required by frontend integration)
        /// </summary>
        [HttpPost("{id}/branches")]
        [Authorize(Policy = "Supplier.Write")]
        public async Task<IActionResult> AssignSupplierToBranches(int id, [FromBody] AssignSupplierToBranchesRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();
                
                if (!currentUserId.HasValue || currentUserId.Value == 0)
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Invalid user session"
                    });
                }

                // Check if supplier exists and user has access
                var supplier = await _context.Suppliers
                    .Include(s => s.Branches)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (supplier == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Supplier not found"
                    });
                }

                // Get user's accessible branches
                var accessibleBranchIds = await GetUserAccessibleBranches(currentUserId.Value, currentUserRole);

                // Validate requested branch IDs
                var requestedBranchIds = request.BranchIds ?? new List<int>();
                var unauthorizedBranches = requestedBranchIds.Except(accessibleBranchIds).ToList();
                
                if (unauthorizedBranches.Any())
                {
                    return StatusCode(403, new
                    {
                        success = false,
                        message = $"Access denied to branches: {string.Join(", ", unauthorizedBranches)}"
                    });
                }

                // Get branches to assign
                var branchesToAssign = await _context.Branches
                    .Where(b => requestedBranchIds.Contains(b.Id) && b.IsActive)
                    .ToListAsync();

                // Clear existing branch assignments
                supplier.Branches.Clear();

                // Add new branch assignments
                foreach (var branch in branchesToAssign)
                {
                    supplier.Branches.Add(branch);
                }

                supplier.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = $"Supplier assigned to {branchesToAssign.Count} branches",
                    data = new
                    {
                        supplierId = supplier.Id,
                        assignedBranches = branchesToAssign.Select(b => new
                        {
                            id = b.Id,
                            name = b.BranchName,
                            type = b.BranchType.ToString()
                        }).ToList()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning supplier {SupplierId} to branches", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get supplier performance across branches (Required by frontend integration)
        /// </summary>
        [HttpGet("{id}/performance")]
        [Authorize(Policy = "Supplier.Read")]
        public async Task<IActionResult> GetSupplierPerformance(int id, [FromQuery] string? branchIds = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();
                
                if (!currentUserId.HasValue || currentUserId.Value == 0)
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Invalid user session"
                    });
                }

                // Parse and validate branch IDs
                var requestedBranchIds = new List<int>();
                if (!string.IsNullOrEmpty(branchIds))
                {
                    requestedBranchIds = branchIds.Split(',')
                        .Where(id => int.TryParse(id.Trim(), out _))
                        .Select(id => int.Parse(id.Trim()))
                        .ToList();
                }

                // Get user's accessible branches
                var accessibleBranchIds = await GetUserAccessibleBranches(currentUserId.Value, currentUserRole);
                
                // Filter requested branches by user access
                if (requestedBranchIds.Any())
                {
                    requestedBranchIds = requestedBranchIds.Intersect(accessibleBranchIds).ToList();
                }
                else
                {
                    requestedBranchIds = accessibleBranchIds;
                }

                // Check if supplier exists and user has access
                var supplier = await _context.Suppliers
                    .Include(s => s.Branches)
                    .Where(s => s.Id == id)
                    .Where(s => s.Branches.Any(sb => accessibleBranchIds.Contains(sb.Id)) || 
                               !s.Branches.Any()) // Include global suppliers
                    .FirstOrDefaultAsync();

                if (supplier == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Supplier not found or not accessible"
                    });
                }

                // Get performance data (last 30 days and previous 30 days for comparison)
                var endDate = DateTime.UtcNow.Date;
                var startDate = endDate.AddDays(-30);
                var previousStartDate = startDate.AddDays(-30);

                // Get purchase orders from this supplier
                var currentPeriodOrders = await _context.PurchaseOrders
                    .Include(po => po.CreatedByUser)
                    .Where(po => po.SupplierId == id)
                    .Where(po => po.CreatedAt >= startDate && po.CreatedAt <= endDate)
                    .Where(po => requestedBranchIds.Contains(po.CreatedByUser.BranchId ?? 0))
                    .ToListAsync();

                var previousPeriodOrders = await _context.PurchaseOrders
                    .Include(po => po.CreatedByUser)
                    .Where(po => po.SupplierId == id)
                    .Where(po => po.CreatedAt >= previousStartDate && po.CreatedAt < startDate)
                    .Where(po => requestedBranchIds.Contains(po.CreatedByUser.BranchId ?? 0))
                    .ToListAsync();

                var totalPurchases = currentPeriodOrders.Sum(po => po.TotalAmount);
                var totalOrders = currentPeriodOrders.Count;
                var averageOrderValue = totalOrders > 0 ? totalPurchases / totalOrders : 0;

                var previousTotalPurchases = previousPeriodOrders.Sum(po => po.TotalAmount);
                var purchaseGrowth = previousTotalPurchases > 0 
                    ? ((totalPurchases - previousTotalPurchases) / previousTotalPurchases) * 100 
                    : 0;

                // Calculate branch breakdown
                var branchPerformance = new List<object>();
                var branches = await _context.Branches
                    .Where(b => requestedBranchIds.Contains(b.Id) && b.IsActive)
                    .ToListAsync();

                foreach (var branch in branches)
                {
                    var branchOrders = currentPeriodOrders
                        .Where(po => po.User?.BranchId == branch.Id)
                        .ToList();

                    var branchTotal = branchOrders.Sum(po => po.TotalAmount);
                    var branchOrderCount = branchOrders.Count;

                    branchPerformance.Add(new
                    {
                        branchId = branch.Id,
                        branchName = branch.BranchName,
                        totalPurchases = branchTotal,
                        orderCount = branchOrderCount,
                        averageOrderValue = branchOrderCount > 0 ? branchTotal / branchOrderCount : 0,
                        lastOrderDate = branchOrders.Any() ? branchOrders.Max(po => po.CreatedAt) : (DateTime?)null
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        supplierId = supplier.Id,
                        supplierName = supplier.Name,
                        totalPurchases = Math.Round(totalPurchases, 2),
                        totalOrders = totalOrders,
                        averageOrderValue = Math.Round(averageOrderValue, 2),
                        purchaseGrowth = Math.Round(purchaseGrowth, 1),
                        currentBalance = supplier.CurrentBalance,
                        creditLimit = supplier.CreditLimit,
                        creditUtilization = supplier.CreditLimit > 0 ? 
                            Math.Round((supplier.CurrentBalance / supplier.CreditLimit) * 100, 1) : 0,
                        branchPerformance = branchPerformance
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving supplier performance {SupplierId}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get comprehensive supplier analytics for dashboard
        /// </summary>
        [HttpGet("analytics")]
        [Authorize(Policy = "Supplier.Read")]
        public async Task<IActionResult> GetSupplierAnalytics([FromQuery] string? branchIds = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();
                
                if (!currentUserId.HasValue || currentUserId.Value == 0)
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Invalid user session"
                    });
                }

                // Parse and validate branch IDs
                var requestedBranchIds = new List<int>();
                if (!string.IsNullOrEmpty(branchIds))
                {
                    requestedBranchIds = branchIds.Split(',')
                        .Where(id => int.TryParse(id.Trim(), out _))
                        .Select(id => int.Parse(id.Trim()))
                        .ToList();
                }

                // Get user's accessible branches
                var accessibleBranchIds = await GetUserAccessibleBranches(currentUserId.Value, currentUserRole);
                
                // Filter requested branches by user access
                if (requestedBranchIds.Any())
                {
                    requestedBranchIds = requestedBranchIds.Intersect(accessibleBranchIds).ToList();
                }
                else
                {
                    requestedBranchIds = accessibleBranchIds;
                }

                // Calculate supplier metrics
                var suppliersQuery = _context.Suppliers
                    .Include(s => s.Branches)
                    .Where(s => s.Branches.Any(sb => requestedBranchIds.Contains(sb.Id)) || 
                               !s.Branches.Any()); // Include global suppliers

                var totalSuppliers = await suppliersQuery.CountAsync();
                var activeSuppliers = await suppliersQuery.Where(s => s.IsActive).CountAsync();
                
                var totalCreditLimit = await suppliersQuery
                    .Where(s => s.IsActive)
                    .SumAsync(s => s.CreditLimit);
                    
                var totalCurrentBalance = await suppliersQuery
                    .Where(s => s.IsActive)
                    .SumAsync(s => s.CurrentBalance);

                // Get outstanding factures
                var outstandingFactures = await _context.Factures
                    .Include(f => f.CreatedByUser)
                    .Where(f => f.CreatedByUser != null && requestedBranchIds.Contains(f.CreatedByUser.BranchId ?? 0))
                    .Where(f => f.Status != FactureStatus.Paid && f.Status != FactureStatus.Cancelled)
                    .CountAsync();

                var outstandingAmount = await _context.Factures
                    .Include(f => f.CreatedByUser)
                    .Where(f => f.CreatedByUser != null && requestedBranchIds.Contains(f.CreatedByUser.BranchId ?? 0))
                    .Where(f => f.Status != FactureStatus.Paid && f.Status != FactureStatus.Cancelled)
                    .SumAsync(f => f.TotalAmount - f.PaidAmount);

                // Generate supplier alerts
                var supplierAlerts = new List<object>();
                
                // Credit limit alerts (>80% utilization)
                var highCreditUtilizationSuppliers = await suppliersQuery
                    .Where(s => s.IsActive && s.CreditLimit > 0)
                    .Where(s => (s.CurrentBalance / s.CreditLimit) > 0.8m)
                    .Select(s => new
                    {
                        s.Id,
                        s.Name,
                        s.CurrentBalance,
                        s.CreditLimit,
                        UtilizationPercentage = (s.CurrentBalance / s.CreditLimit) * 100
                    })
                    .ToListAsync();

                foreach (var supplier in highCreditUtilizationSuppliers)
                {
                    supplierAlerts.Add(new
                    {
                        id = supplier.Id,
                        type = "credit_limit",
                        severity = supplier.UtilizationPercentage > 95 ? "high" : "medium",
                        title = "High Credit Utilization",
                        message = $"{supplier.Name} credit utilization: {supplier.UtilizationPercentage:F1}%",
                        supplierName = supplier.Name,
                        currentBalance = supplier.CurrentBalance,
                        creditLimit = supplier.CreditLimit
                    });
                }

                // Payment overdue alerts
                var overduePayments = await _context.Factures
                    .Include(f => f.Supplier)
                    .Include(f => f.CreatedByUser)
                    .Where(f => f.CreatedByUser != null && requestedBranchIds.Contains(f.CreatedByUser.BranchId ?? 0))
                    .Where(f => f.DueDate < DateTime.UtcNow.Date)
                    .Where(f => f.Status != FactureStatus.Paid && f.Status != FactureStatus.Cancelled)
                    .GroupBy(f => f.Supplier)
                    .Where(g => g.Key != null)
                    .Select(g => new
                    {
                        SupplierId = g.Key!.Id,
                        SupplierName = g.Key!.Name,
                        OverdueCount = g.Count(),
                        OverdueAmount = g.Sum(f => f.TotalAmount - f.PaidAmount)
                    })
                    .ToListAsync();

                foreach (var overdue in overduePayments)
                {
                    supplierAlerts.Add(new
                    {
                        id = overdue.SupplierId,
                        type = "payment_overdue",
                        severity = overdue.OverdueAmount > 10000000 ? "high" : "medium", // 10M IDR threshold
                        title = "Overdue Payments",
                        message = $"{overdue.SupplierName} has {overdue.OverdueCount} overdue payment(s)",
                        supplierName = overdue.SupplierName,
                        overdueCount = overdue.OverdueCount,
                        overdueAmount = overdue.OverdueAmount
                    });
                }

                // Top suppliers by factures (last 30 days)
                var startDate = DateTime.UtcNow.Date.AddDays(-30);
                var topSuppliersByFactures = await _context.Factures
                    .Include(f => f.Supplier)
                    .Include(f => f.CreatedByUser)
                    .Where(f => f.CreatedByUser != null && requestedBranchIds.Contains(f.CreatedByUser.BranchId ?? 0))
                    .Where(f => f.CreatedAt >= startDate)
                    .Where(f => f.Supplier != null)
                    .GroupBy(f => f.Supplier)
                    .Where(g => g.Key != null)
                    .Select(g => new
                    {
                        supplierId = g.Key!.Id,
                        supplierName = g.Key!.Name,
                        totalFactures = g.Count(),
                        totalAmount = g.Sum(f => f.TotalAmount),
                        averageAmount = g.Average(f => f.TotalAmount)
                    })
                    .OrderByDescending(s => s.totalAmount)
                    .Take(10)
                    .ToListAsync();

                // Suppliers by branch
                var suppliersByBranch = new List<object>();
                var branches = await _context.Branches
                    .Where(b => requestedBranchIds.Contains(b.Id) && b.IsActive)
                    .ToListAsync();

                foreach (var branch in branches)
                {
                    var branchSupplierCount = await _context.Suppliers
                        .Where(s => s.Branches.Any(sb => sb.Id == branch.Id) || !s.Branches.Any())
                        .Where(s => s.IsActive)
                        .CountAsync();

                    suppliersByBranch.Add(new
                    {
                        branchId = branch.Id,
                        branchName = branch.BranchName,
                        supplierCount = branchSupplierCount
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        totalSuppliers = totalSuppliers,
                        activeSuppliers = activeSuppliers,
                        inactiveSuppliers = totalSuppliers - activeSuppliers,
                        totalCreditLimit = Math.Round(totalCreditLimit, 2),
                        totalCurrentBalance = Math.Round(totalCurrentBalance, 2),
                        creditUtilization = totalCreditLimit > 0 ? Math.Round((totalCurrentBalance / totalCreditLimit) * 100, 1) : 0,
                        outstandingFactures = outstandingFactures,
                        outstandingAmount = Math.Round(outstandingAmount, 2),
                        supplierAlerts = supplierAlerts,
                        topSuppliersByFactures = topSuppliersByFactures,
                        suppliersByBranch = suppliersByBranch
                    },
                    message = "Supplier analytics retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving supplier analytics");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error"
                });
            }
        }

        // ==================== REQUEST MODELS ==================== //

        public class AssignSupplierToBranchesRequest
        {
            public List<int>? BranchIds { get; set; }
        }

        // ==================== HELPER METHODS ==================== //

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value ?? 
                             User.FindFirst("sub")?.Value ?? 
                             User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        private string GetCurrentUserRole()
        {
            return User.FindFirst("Role")?.Value ?? 
                   User.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value ?? 
                   User.FindFirst(ClaimTypes.Role)?.Value ??
                   "User";
        }

        private async Task<List<int>> GetUserAccessibleBranches(int userId, string userRole)
        {
            var accessibleBranches = new List<int>();

            if (userRole.ToUpper() is "ADMIN" or "HEADMANAGER")
            {
                // Admin and HeadManager can access all branches
                accessibleBranches = await _context.Branches
                    .Where(b => b.IsActive)
                    .Select(b => b.Id)
                    .ToListAsync();
            }
            else
            {
                try
                {
                    // Get user's accessible branches via BranchAccess table
                    accessibleBranches = await _context.BranchAccesses
                        .Where(ba => ba.UserId == userId && ba.IsActive && ba.CanRead)
                        .Select(ba => ba.BranchId)
                        .ToListAsync();
                }
                catch
                {
                    // Fallback: Use user's assigned branch
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                    if (user?.BranchId.HasValue == true)
                    {
                        accessibleBranches.Add(user.BranchId.Value);
                    }
                }
            }

            return accessibleBranches;
        }
    }
}