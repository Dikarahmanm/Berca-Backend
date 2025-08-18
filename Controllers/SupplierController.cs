using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using Berca_Backend.DTOs;
using Berca_Backend.Services.Interfaces;
using Berca_Backend.Models;

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
        private readonly ILogger<SupplierController> _logger;

        public SupplierController(
            ISupplierService supplierService,
            ILogger<SupplierController> logger)
        {
            _supplierService = supplierService;
            _logger = logger;
        }

        // ==================== CRUD ENDPOINTS ==================== //

        /// <summary>
        /// Get suppliers with filtering, searching and pagination
        /// </summary>
        /// <param name="queryParams">Query parameters for filtering</param>
        /// <returns>Paginated supplier list</returns>
        [HttpGet]
        [Authorize(Policy = "Supplier.Read")]
        public async Task<IActionResult> GetSuppliers([FromQuery] SupplierQueryDto queryParams)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                    return Unauthorized(ApiResponse<object>.ErrorResponse("User not authenticated"));

                var queryModel = new SupplierQueryParams
                {
                    Search = queryParams.Search,
                    BranchId = queryParams.BranchId,
                    IsActive = queryParams.IsActive,
                    MinPaymentTerms = queryParams.MinPaymentTerms,
                    MaxPaymentTerms = queryParams.MaxPaymentTerms,
                    MinCreditLimit = queryParams.MinCreditLimit,
                    MaxCreditLimit = queryParams.MaxCreditLimit,
                    Page = queryParams.Page,
                    PageSize = Math.Min(queryParams.PageSize, 100), // Limit page size
                    SortBy = queryParams.SortBy,
                    SortOrder = queryParams.SortOrder
                };

                var result = await _supplierService.GetSuppliersAsync(queryModel, currentUserId.Value);

                var message = $"Retrieved {result.Suppliers.Count} suppliers (page {result.Page} of {result.TotalPages})";
                return Ok(ApiResponse<SupplierPagedResponseDto>.SuccessResponse(result, message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving suppliers");
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Get supplier by ID
        /// </summary>
        /// <param name="id">Supplier ID</param>
        /// <returns>Supplier details</returns>
        [HttpGet("{id}")]
        [Authorize(Policy = "Supplier.Read")]
        public async Task<IActionResult> GetSupplierById(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                    return Unauthorized(ApiResponse<SupplierDto>.ErrorResponse("User not authenticated"));

                var supplier = await _supplierService.GetSupplierByIdAsync(id, currentUserId.Value);
                if (supplier == null)
                    return NotFound(ApiResponse<SupplierDto>.ErrorResponse("Supplier not found"));

                return Ok(ApiResponse<SupplierDto>.SuccessResponse(supplier, "Supplier retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving supplier {SupplierId}", id);
                return StatusCode(500, ApiResponse<SupplierDto>.ErrorResponse("Internal server error"));
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

        // ==================== HELPER METHODS ==================== //

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out var userId))
                return userId;

            // For testing purposes - return a default admin user ID when not authenticated
            return 5; // dikdika user ID for testing
        }

        private string GetCurrentUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value ?? "Unknown";
        }

        private bool IsUserAuthorizedForSupplier(string requiredRole)
        {
            var userRole = GetCurrentUserRole();
            return requiredRole switch
            {
                "Admin" => userRole == "Admin",
                "Manager" => userRole is "Admin" or "HeadManager" or "BranchManager",
                "User" => userRole is "Admin" or "HeadManager" or "BranchManager" or "User",
                _ => false
            };
        }
    }
}