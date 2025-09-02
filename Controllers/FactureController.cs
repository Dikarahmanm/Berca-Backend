using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Berca_Backend.DTOs;
using Berca_Backend.Services.Interfaces;
using Berca_Backend.Models;
using Berca_Backend.Data;
using System.Security.Claims;

namespace Berca_Backend.Controllers
{
    /// <summary>
    /// Facture Management Controller
    /// Handles supplier invoice receiving, verification, and payment workflows
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class FactureController : ControllerBase
    {
        private readonly IFactureService _factureService;
        private readonly ILogger<FactureController> _logger;
        private readonly AppDbContext _context;

        public FactureController(IFactureService factureService, ILogger<FactureController> logger, AppDbContext context)
        {
            _factureService = factureService;
            _logger = logger;
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value ?? 
                             User.FindFirst("sub")?.Value ?? 
                             User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
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

        // ==================== FACTURE RECEIVING & WORKFLOW ==================== //

        /// <summary>
        /// Receive new supplier invoice and create facture record
        /// </summary>
        [HttpPost("receive")]
        [Authorize(Policy = "Facture.Receive")]
        public async Task<ActionResult<FactureDto>> ReceiveSupplierInvoice([FromBody] ReceiveFactureDto receiveDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var currentUserId = GetCurrentUserId();
                var facture = await _factureService.ReceiveSupplierInvoiceAsync(receiveDto, currentUserId);

                _logger.LogInformation("Supplier invoice {SupplierInvoiceNumber} received by user {UserId}", 
                    receiveDto.SupplierInvoiceNumber, currentUserId);

                return CreatedAtAction(nameof(GetFactureById), new { id = facture.Id }, facture);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving supplier invoice");
                return StatusCode(500, new { message = "An error occurred while receiving the supplier invoice." });
            }
        }

        /// <summary>
        /// Verify facture items against delivery
        /// </summary>
        [HttpPost("{id}/verify")]
        [Authorize(Policy = "Facture.Verify")]
        public async Task<ActionResult<FactureDto>> VerifyFactureItems(int id, [FromBody] VerifyFactureDto verifyDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (id != verifyDto.FactureId)
                    return BadRequest(new { message = "Facture ID mismatch." });

                var currentUserId = GetCurrentUserId();
                var facture = await _factureService.VerifyFactureItemsAsync(verifyDto, currentUserId);

                if (facture == null)
                    return NotFound(new { message = "Facture not found or cannot be verified." });

                _logger.LogInformation("Facture {FactureId} verified by user {UserId}", id, currentUserId);

                return Ok(facture);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying facture {FactureId}", id);
                return StatusCode(500, new { message = "An error occurred while verifying the facture." });
            }
        }

        /// <summary>
        /// Approve facture for payment processing
        /// </summary>
        [HttpPost("{id}/approve")]
        [Authorize(Policy = "Facture.Approve")]
        public async Task<ActionResult<FactureDto>> ApproveFacture(int id, [FromBody] string? approvalNotes = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var facture = await _factureService.ApproveFactureAsync(id, currentUserId, approvalNotes);

                if (facture == null)
                    return NotFound(new { message = "Facture not found or cannot be approved." });

                _logger.LogInformation("Facture {FactureId} approved by user {UserId}", id, currentUserId);

                return Ok(facture);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving facture {FactureId}", id);
                return StatusCode(500, new { message = "An error occurred while approving the facture." });
            }
        }

        /// <summary>
        /// Dispute facture with supplier
        /// </summary>
        [HttpPost("{id}/dispute")]
        [Authorize(Policy = "Facture.Dispute")]
        public async Task<ActionResult<FactureDto>> DisputeFacture(int id, [FromBody] DisputeFactureDto disputeDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (id != disputeDto.FactureId)
                    return BadRequest(new { message = "Facture ID mismatch." });

                var currentUserId = GetCurrentUserId();
                var facture = await _factureService.DisputeFactureAsync(disputeDto, currentUserId);

                if (facture == null)
                    return NotFound(new { message = "Facture not found or cannot be disputed." });

                _logger.LogInformation("Facture {FactureId} disputed by user {UserId}", id, currentUserId);

                return Ok(facture);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disputing facture {FactureId}", id);
                return StatusCode(500, new { message = "An error occurred while disputing the facture." });
            }
        }

        /// <summary>
        /// Cancel/reject facture
        /// </summary>
        [HttpPost("{id}/cancel")]
        [Authorize(Policy = "Facture.Cancel")]
        public async Task<ActionResult> CancelFacture(int id, [FromBody] string? cancellationReason = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var result = await _factureService.CancelFactureAsync(id, currentUserId, cancellationReason);

                if (!result)
                    return NotFound(new { message = "Facture not found or cannot be cancelled." });

                _logger.LogInformation("Facture {FactureId} cancelled by user {UserId}", id, currentUserId);

                return Ok(new { message = "Facture cancelled successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling facture {FactureId}", id);
                return StatusCode(500, new { message = "An error occurred while cancelling the facture." });
            }
        }

        // ==================== FACTURE CRUD OPERATIONS ==================== //

        /// <summary>
        /// Get factures with filtering, searching and pagination (Required by frontend integration)
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "Facture.Read")]
        public async Task<IActionResult> GetFactures(
            [FromQuery] string? search = null,
            [FromQuery] string? branchIds = null,
            [FromQuery] string? status = null,
            [FromQuery] int? supplierId = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string sortBy = "CreatedAt",
            [FromQuery] string sortOrder = "desc")
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();
                
                if (currentUserId == 0)
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
                var accessibleBranchIds = await GetUserAccessibleBranches(currentUserId, currentUserRole);
                
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

                // Build factures query with branch filtering
                var facturesQuery = _context.Factures
                    .Include(f => f.Supplier)
                    .Include(f => f.CreatedByUser)
                    .ThenInclude(u => u.Branch)
                    .Where(f => requestedBranchIds.Contains(f.CreatedByUser.BranchId ?? 0));

                // Apply search filter
                if (!string.IsNullOrEmpty(search))
                {
                    facturesQuery = facturesQuery.Where(f =>
                        (f.SupplierInvoiceNumber != null && f.SupplierInvoiceNumber.Contains(search)) ||
                        (f.InternalReferenceNumber != null && f.InternalReferenceNumber.Contains(search)) ||
                        (f.Supplier != null && f.Supplier.Name != null && f.Supplier.Name.Contains(search)) ||
                        (f.Supplier != null && f.Supplier.CompanyName != null && f.Supplier.CompanyName.Contains(search)));
                }

                // Apply status filter
                if (!string.IsNullOrEmpty(status))
                {
                    if (Enum.TryParse<FactureStatus>(status, true, out var factureStatus))
                    {
                        facturesQuery = facturesQuery.Where(f => f.Status == factureStatus);
                    }
                }

                // Apply supplier filter
                if (supplierId.HasValue)
                {
                    facturesQuery = facturesQuery.Where(f => f.SupplierId == supplierId.Value);
                }

                // Apply date filters
                if (fromDate.HasValue)
                {
                    facturesQuery = facturesQuery.Where(f => f.InvoiceDate >= fromDate.Value.Date);
                }
                if (toDate.HasValue)
                {
                    facturesQuery = facturesQuery.Where(f => f.InvoiceDate <= toDate.Value.Date);
                }

                // Apply sorting
                facturesQuery = sortBy.ToLower() switch
                {
                    "invoicedate" => sortOrder.ToLower() == "desc" ? 
                        facturesQuery.OrderByDescending(f => f.InvoiceDate) : 
                        facturesQuery.OrderBy(f => f.InvoiceDate),
                    "duedate" => sortOrder.ToLower() == "desc" ? 
                        facturesQuery.OrderByDescending(f => f.DueDate) : 
                        facturesQuery.OrderBy(f => f.DueDate),
                    "totalamount" => sortOrder.ToLower() == "desc" ? 
                        facturesQuery.OrderByDescending(f => f.TotalAmount) : 
                        facturesQuery.OrderBy(f => f.TotalAmount),
                    "status" => sortOrder.ToLower() == "desc" ? 
                        facturesQuery.OrderByDescending(f => f.Status) : 
                        facturesQuery.OrderBy(f => f.Status),
                    _ => sortOrder.ToLower() == "desc" ? 
                        facturesQuery.OrderByDescending(f => f.CreatedAt) : 
                        facturesQuery.OrderBy(f => f.CreatedAt)
                };

                // Get total count
                var totalCount = await facturesQuery.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                // Apply pagination
                var factures = await facturesQuery
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(f => new
                    {
                        id = f.Id,
                        supplierInvoiceNumber = f.SupplierInvoiceNumber,
                        internalReferenceNumber = f.InternalReferenceNumber,
                        supplier = f.Supplier != null ? new
                        {
                            id = f.Supplier.Id,
                            name = f.Supplier.Name,
                            companyName = f.Supplier.CompanyName
                        } : null,
                        invoiceDate = f.InvoiceDate,
                        dueDate = f.DueDate,
                        totalAmount = f.TotalAmount,
                        paidAmount = f.PaidAmount,
                        remainingAmount = f.TotalAmount - f.PaidAmount,
                        status = f.Status.ToString(),
                        branch = f.CreatedByUser != null && f.CreatedByUser.Branch != null ? new
                        {
                            id = f.CreatedByUser.Branch.Id,
                            name = f.CreatedByUser.Branch.BranchName
                        } : null,
                        createdAt = f.CreatedAt,
                        updatedAt = f.UpdatedAt
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = factures,
                    pagination = new
                    {
                        currentPage = page,
                        totalPages = totalPages,
                        totalItems = totalCount,
                        itemsPerPage = pageSize
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving factures");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get facture by ID with access validation (Required by frontend integration)
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Policy = "Facture.Read")]
        public async Task<IActionResult> GetFactureById(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();
                
                if (currentUserId == 0)
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Invalid user session"
                    });
                }

                // Get user's accessible branches
                var accessibleBranchIds = await GetUserAccessibleBranches(currentUserId, currentUserRole);

                var facture = await _context.Factures
                    .Include(f => f.Supplier)
                    .Include(f => f.CreatedByUser)
                    .ThenInclude(u => u.Branch)
                    .Include(f => f.Items)
                    .ThenInclude(fi => fi.Product)
                    .Where(f => f.Id == id)
                    .Where(f => accessibleBranchIds.Contains(f.CreatedByUser.BranchId ?? 0))
                    .Select(f => new
                    {
                        id = f.Id,
                        supplierInvoiceNumber = f.SupplierInvoiceNumber,
                        internalReferenceNumber = f.InternalReferenceNumber,
                        supplier = f.Supplier != null ? new
                        {
                            id = f.Supplier.Id,
                            name = f.Supplier.Name,
                            companyName = f.Supplier.CompanyName,
                            email = f.Supplier.Email,
                            phone = f.Supplier.Phone
                        } : null,
                        invoiceDate = f.InvoiceDate,
                        dueDate = f.DueDate,
                        deliveryDate = f.DeliveryDate,
                        totalAmount = f.TotalAmount,
                        paidAmount = f.PaidAmount,
                        remainingAmount = f.TotalAmount - f.PaidAmount,
                        taxAmount = f.Tax,
                        discountAmount = f.Discount,
                        status = f.Status.ToString(),
                        notes = f.Notes,
                        items = f.Items.Select(fi => new
                        {
                            id = fi.Id,
                            product = fi.Product != null ? new
                            {
                                id = fi.Product.Id,
                                name = fi.Product.Name,
                                barcode = fi.Product.Barcode
                            } : null,
                            quantity = fi.Quantity,
                            unitPrice = fi.UnitPrice,
                            totalPrice = fi.TotalPrice,
                            receivedQuantity = fi.ReceivedQuantity,
                            verifiedQuantity = fi.VerifiedQuantity
                        }).ToList(),
                        branch = f.CreatedByUser != null && f.CreatedByUser.Branch != null ? new
                        {
                            id = f.CreatedByUser.Branch.Id,
                            name = f.CreatedByUser.Branch.BranchName,
                            type = f.CreatedByUser.Branch.BranchType.ToString()
                        } : null,
                        createdBy = f.CreatedByUser != null ? new
                        {
                            id = f.CreatedByUser.Id,
                            name = f.CreatedByUser.Name
                        } : null,
                        createdAt = f.CreatedAt,
                        updatedAt = f.UpdatedAt
                    })
                    .FirstOrDefaultAsync();

                if (facture == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Facture not found or not accessible"
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = facture
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving facture {FactureId}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get facture by supplier invoice number
        /// </summary>
        [HttpGet("supplier-invoice/{supplierInvoiceNumber}")]
        [Authorize(Policy = "Facture.Read")]
        public async Task<ActionResult<FactureDto>> GetFactureBySupplierInvoiceNumber(string supplierInvoiceNumber, [FromQuery] int supplierId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var facture = await _factureService.GetFactureBySupplierInvoiceNumberAsync(supplierInvoiceNumber, supplierId, currentUserId);

                if (facture == null)
                    return NotFound(new { message = "Facture not found." });

                return Ok(facture);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving facture by supplier invoice number {SupplierInvoiceNumber}", supplierInvoiceNumber);
                return StatusCode(500, new { message = "An error occurred while retrieving the facture." });
            }
        }

        /// <summary>
        /// Update facture details (limited based on status)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Policy = "Facture.Update")]
        public async Task<ActionResult<FactureDto>> UpdateFacture(int id, [FromBody] UpdateFactureDto updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var currentUserId = GetCurrentUserId();
                var facture = await _factureService.UpdateFactureAsync(id, updateDto, currentUserId);

                if (facture == null)
                    return NotFound(new { message = "Facture not found or cannot be updated." });

                _logger.LogInformation("Facture {FactureId} updated by user {UserId}", id, currentUserId);

                return Ok(facture);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating facture {FactureId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the facture." });
            }
        }

        // ==================== SUPPLIER-SPECIFIC OPERATIONS ==================== //

        /// <summary>
        /// Get factures for specific supplier
        /// </summary>
        [HttpGet("supplier/{supplierId}")]
        [Authorize(Policy = "Facture.Read")]
        public async Task<ActionResult<List<FactureListDto>>> GetSupplierFactures(int supplierId, [FromQuery] bool includeCompleted = false, [FromQuery] int pageSize = 50)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var factures = await _factureService.GetSupplierFacturesAsync(supplierId, currentUserId, includeCompleted, pageSize);

                return Ok(factures);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving supplier factures for supplier {SupplierId}", supplierId);
                return StatusCode(500, new { message = "An error occurred while retrieving supplier factures." });
            }
        }

        /// <summary>
        /// Get supplier facture summary with outstanding balances
        /// </summary>
        [HttpGet("supplier/{supplierId}/summary")]
        [Authorize(Policy = "Facture.Read")]
        public async Task<ActionResult<SupplierFactureSummaryDto>> GetSupplierFactureSummary(int supplierId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var summary = await _factureService.GetSupplierFactureSummaryAsync(supplierId, currentUserId);

                if (summary == null)
                    return NotFound(new { message = "Supplier not found." });

                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving supplier facture summary for supplier {SupplierId}", supplierId);
                return StatusCode(500, new { message = "An error occurred while retrieving supplier summary." });
            }
        }

        // ==================== PAYMENT MANAGEMENT ==================== //

        /// <summary>
        /// Schedule payment for facture
        /// </summary>
        [HttpPost("{id}/payments/schedule")]
        [Authorize(Policy = "Facture.SchedulePayment")]
        public async Task<ActionResult<FacturePaymentDto>> SchedulePayment(int id, [FromBody] SchedulePaymentDto scheduleDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (id != scheduleDto.FactureId)
                    return BadRequest(new { message = "Facture ID mismatch." });

                var currentUserId = GetCurrentUserId();
                var payment = await _factureService.SchedulePaymentAsync(scheduleDto, currentUserId);

                _logger.LogInformation("Payment scheduled for facture {FactureId} by user {UserId}", id, currentUserId);

                return CreatedAtAction(nameof(GetFacturePayments), new { id = id }, payment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling payment for facture {FactureId}", id);
                return StatusCode(500, new { message = "An error occurred while scheduling the payment." });
            }
        }

        /// <summary>
        /// Process scheduled payment
        /// </summary>
        [HttpPost("payments/{paymentId}/process")]
        [Authorize(Policy = "Facture.ProcessPayment")]
        public async Task<ActionResult<FacturePaymentDto>> ProcessPayment(int paymentId, [FromBody] ProcessPaymentDto processDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (paymentId != processDto.PaymentId)
                    return BadRequest(new { message = "Payment ID mismatch." });

                var currentUserId = GetCurrentUserId();
                var payment = await _factureService.ProcessPaymentAsync(processDto, currentUserId);

                if (payment == null)
                    return NotFound(new { message = "Payment not found or cannot be processed." });

                _logger.LogInformation("Payment {PaymentId} processed by user {UserId}", paymentId, currentUserId);

                return Ok(payment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment {PaymentId}", paymentId);
                return StatusCode(500, new { message = "An error occurred while processing the payment." });
            }
        }

        /// <summary>
        /// Confirm payment received by supplier
        /// </summary>
        [HttpPost("payments/{paymentId}/confirm")]
        [Authorize(Policy = "Facture.ConfirmPayment")]
        public async Task<ActionResult<FacturePaymentDto>> ConfirmPayment(int paymentId, [FromBody] ConfirmPaymentDto confirmDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (paymentId != confirmDto.PaymentId)
                    return BadRequest(new { message = "Payment ID mismatch." });

                var currentUserId = GetCurrentUserId();
                var payment = await _factureService.ConfirmPaymentAsync(confirmDto, currentUserId);

                if (payment == null)
                    return NotFound(new { message = "Payment not found or cannot be confirmed." });

                _logger.LogInformation("Payment {PaymentId} confirmed by user {UserId}", paymentId, currentUserId);

                return Ok(payment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming payment {PaymentId}", paymentId);
                return StatusCode(500, new { message = "An error occurred while confirming the payment." });
            }
        }

        /// <summary>
        /// Get payment history for facture
        /// </summary>
        [HttpGet("{id}/payments")]
        [Authorize(Policy = "Facture.Read")]
        public async Task<ActionResult<List<FacturePaymentDto>>> GetFacturePayments(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var payments = await _factureService.GetFacturePaymentsAsync(id, currentUserId);

                return Ok(payments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving payments for facture {FactureId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving payments." });
            }
        }

        /// <summary>
        /// Update payment details
        /// </summary>
        [HttpPut("payments/{paymentId}")]
        [Authorize(Policy = "Facture.UpdatePayment")]
        public async Task<ActionResult<FacturePaymentDto>> UpdatePayment(int paymentId, [FromBody] UpdatePaymentRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var payment = await _factureService.UpdatePaymentAsync(paymentId, currentUserId, request.Notes, request.BankAccount);

                if (payment == null)
                    return NotFound(new { message = "Payment not found or cannot be updated." });

                _logger.LogInformation("Payment {PaymentId} updated by user {UserId}", paymentId, currentUserId);

                return Ok(payment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating payment {PaymentId}", paymentId);
                return StatusCode(500, new { message = "An error occurred while updating the payment." });
            }
        }

        /// <summary>
        /// Cancel payment
        /// </summary>
        [HttpPost("payments/{paymentId}/cancel")]
        [Authorize(Policy = "Facture.CancelPayment")]
        public async Task<ActionResult> CancelPayment(int paymentId, [FromBody] string? cancellationReason = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var result = await _factureService.CancelPaymentAsync(paymentId, currentUserId, cancellationReason);

                if (!result)
                    return NotFound(new { message = "Payment not found or cannot be cancelled." });

                _logger.LogInformation("Payment {PaymentId} cancelled by user {UserId}", paymentId, currentUserId);

                return Ok(new { message = "Payment cancelled successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling payment {PaymentId}", paymentId);
                return StatusCode(500, new { message = "An error occurred while cancelling the payment." });
            }
        }

        // ==================== WORKFLOW & STATUS TRACKING ==================== //

        /// <summary>
        /// Get factures pending verification
        /// </summary>
        [HttpGet("pending-verification")]
        [Authorize(Policy = "Facture.Read")]
        public async Task<ActionResult<List<FactureListDto>>> GetFacturesPendingVerification([FromQuery] int? branchId = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var factures = await _factureService.GetFacturesPendingVerificationAsync(currentUserId, branchId);

                return Ok(factures);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving factures pending verification");
                return StatusCode(500, new { message = "An error occurred while retrieving pending factures." });
            }
        }

        /// <summary>
        /// Get factures pending approval
        /// </summary>
        [HttpGet("pending-approval")]
        [Authorize(Policy = "Facture.Read")]
        public async Task<ActionResult<List<FactureListDto>>> GetFacturesPendingApproval([FromQuery] int? branchId = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var factures = await _factureService.GetFacturesPendingApprovalAsync(currentUserId, branchId);

                return Ok(factures);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving factures pending approval");
                return StatusCode(500, new { message = "An error occurred while retrieving pending factures." });
            }
        }

        /// <summary>
        /// Get overdue payments requiring attention
        /// </summary>
        [HttpGet("overdue-payments")]
        [Authorize(Policy = "Facture.Read")]
        public async Task<ActionResult<List<FactureListDto>>> GetOverduePayments([FromQuery] int? branchId = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var factures = await _factureService.GetOverduePaymentsAsync(currentUserId, branchId);

                return Ok(factures);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving overdue payments");
                return StatusCode(500, new { message = "An error occurred while retrieving overdue payments." });
            }
        }

        /// <summary>
        /// Get payments due soon
        /// </summary>
        [HttpGet("payments-due-soon")]
        [Authorize(Policy = "Facture.Read")]
        public async Task<ActionResult<List<FactureListDto>>> GetPaymentsDueSoon([FromQuery] int daysAhead = 7, [FromQuery] int? branchId = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var factures = await _factureService.GetPaymentsDueSoonAsync(currentUserId, daysAhead, branchId);

                return Ok(factures);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving payments due soon");
                return StatusCode(500, new { message = "An error occurred while retrieving payments due soon." });
            }
        }

        /// <summary>
        /// Get payments due today
        /// </summary>
        [HttpGet("payments-due-today")]
        [Authorize(Policy = "Facture.Read")]
        public async Task<ActionResult<List<FactureListDto>>> GetPaymentsDueToday([FromQuery] int? branchId = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var factures = await _factureService.GetPaymentsDueTodayAsync(currentUserId, branchId);

                return Ok(factures);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving payments due today");
                return StatusCode(500, new { message = "An error occurred while retrieving payments due today." });
            }
        }

        // ==================== ANALYTICS & REPORTING ==================== //

        /// <summary>
        /// Get facture summary statistics for dashboard
        /// </summary>
        [HttpGet("summary")]
        [Authorize(Policy = "Facture.Read")]
        public async Task<ActionResult<FactureSummaryDto>> GetFactureSummary([FromQuery] int? branchId = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var summary = await _factureService.GetFactureSummaryAsync(currentUserId, branchId);

                return Ok(summary);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Translation of member") || ex.Message.Contains("LINQ"))
            {
                _logger.LogError(ex, "LINQ translation error in facture summary - this should not happen after fixes");
                return StatusCode(500, new { 
                    message = "Database query translation error. Please contact system administrator.",
                    error = "LINQ_TRANSLATION_ERROR"
                });
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error retrieving facture summary");
                return StatusCode(500, new { 
                    message = "Database error occurred while retrieving summary.",
                    error = "DATABASE_ERROR"
                });
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "Timeout error retrieving facture summary");
                return StatusCode(500, new { 
                    message = "Request timed out. Please try again.",
                    error = "TIMEOUT_ERROR"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving facture summary");
                return StatusCode(500, new { 
                    message = "An unexpected error occurred while retrieving summary.",
                    error = "GENERAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Get payment analytics for cash flow planning
        /// </summary>
        [HttpGet("payment-analytics")]
        [Authorize(Policy = "Facture.Read")]
        public async Task<ActionResult<object>> GetPaymentAnalytics([FromQuery] int daysAhead = 30, [FromQuery] int? branchId = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var analytics = await _factureService.GetPaymentAnalyticsAsync(currentUserId, daysAhead, branchId);

                return Ok(analytics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving payment analytics");
                return StatusCode(500, new { message = "An error occurred while retrieving payment analytics." });
            }
        }

        /// <summary>
        /// Get aging analysis for outstanding payments
        /// </summary>
        [HttpGet("aging-analysis")]
        [Authorize(Policy = "Facture.Read")]
        public async Task<ActionResult<object>> GetAgingAnalysis([FromQuery] int? branchId = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var analysis = await _factureService.GetAgingAnalysisAsync(currentUserId, branchId);

                return Ok(analysis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving aging analysis");
                return StatusCode(500, new { message = "An error occurred while retrieving aging analysis." });
            }
        }

        /// <summary>
        /// Get outstanding factures analytics
        /// </summary>
        [HttpGet("outstanding-factures")]
        [Authorize(Policy = "Facture.Read")]
        public async Task<ActionResult<List<FactureListDto>>> GetOutstandingFactures(
            [FromQuery] int? branchId = null, 
            [FromQuery] int? supplierId = null, 
            [FromQuery] int limit = 50)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var outstandingFactures = await _factureService.GetOutstandingFacturesAsync(currentUserId, branchId, supplierId, limit);

                return Ok(outstandingFactures);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving outstanding factures");
                return StatusCode(500, new { message = "An error occurred while retrieving outstanding factures." });
            }
        }

        /// <summary>
        /// Get top suppliers by factures analytics
        /// </summary>
        [HttpGet("top-suppliers-by-factures")]
        [Authorize(Policy = "Facture.Read")]
        public async Task<ActionResult<List<OutstandingBySupplierDto>>> GetTopSuppliersByFactures(
            [FromQuery] int? branchId = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int limit = 10)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var topSuppliers = await _factureService.GetTopSuppliersByFacturesAsync(currentUserId, branchId, fromDate, toDate, limit);

                return Ok(topSuppliers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving top suppliers by factures");
                return StatusCode(500, new { message = "An error occurred while retrieving top suppliers analytics." });
            }
        }

        /// <summary>
        /// Get suppliers by branch analytics
        /// </summary>
        [HttpGet("suppliers-by-branch")]
        [Authorize(Policy = "Facture.Read")]
        public async Task<ActionResult<List<SuppliersByBranchDto>>> GetSuppliersByBranch(
            [FromQuery] int? branchId = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var suppliersByBranch = await _factureService.GetSuppliersByBranchAsync(currentUserId, branchId);

                return Ok(suppliersByBranch);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving suppliers by branch");
                return StatusCode(500, new { message = "An error occurred while retrieving suppliers by branch analytics." });
            }
        }

        /// <summary>
        /// Get supplier alerts system
        /// </summary>
        [HttpGet("supplier-alerts")]
        [Authorize(Policy = "Facture.Read")]
        public async Task<ActionResult<SupplierAlertsDto>> GetSupplierAlerts(
            [FromQuery] int? branchId = null,
            [FromQuery] string? priorityFilter = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var alerts = await _factureService.GetSupplierAlertsAsync(currentUserId, branchId, priorityFilter);

                return Ok(alerts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving supplier alerts");
                return StatusCode(500, new { message = "An error occurred while retrieving supplier alerts." });
            }
        }

        // ==================== DELIVERY & RECEIVING WORKFLOW ==================== //

        /// <summary>
        /// Record delivery receipt for facture
        /// </summary>
        [HttpPost("{id}/record-delivery")]
        [Authorize(Policy = "Facture.Update")]
        public async Task<ActionResult<FactureDto>> RecordDelivery(int id, [FromBody] RecordDeliveryRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var currentUserId = GetCurrentUserId();
                var facture = await _factureService.RecordDeliveryAsync(id, request.DeliveryDate, request.DeliveryNoteNumber, currentUserId);

                if (facture == null)
                    return NotFound(new { message = "Facture not found." });

                _logger.LogInformation("Delivery recorded for facture {FactureId} by user {UserId}", id, currentUserId);

                return Ok(facture);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording delivery for facture {FactureId}", id);
                return StatusCode(500, new { message = "An error occurred while recording delivery." });
            }
        }

        /// <summary>
        /// Request clarification from supplier
        /// </summary>
        [HttpPost("{id}/request-clarification")]
        [Authorize(Policy = "Facture.Update")]
        public async Task<ActionResult> RequestClarification(int id, [FromBody] RequestClarificationRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var currentUserId = GetCurrentUserId();
                var result = await _factureService.RequestClarificationAsync(id, request.ClarificationReason, currentUserId);

                if (!result)
                    return NotFound(new { message = "Facture not found." });

                _logger.LogInformation("Clarification requested for facture {FactureId} by user {UserId}", id, currentUserId);

                return Ok(new { message = "Clarification request sent successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting clarification for facture {FactureId}", id);
                return StatusCode(500, new { message = "An error occurred while requesting clarification." });
            }
        }

        // ==================== VALIDATION ==================== //

        /// <summary>
        /// Validate supplier invoice number uniqueness
        /// </summary>
        [HttpGet("validate-invoice-number")]
        [Authorize(Policy = "Facture.Read")]
        public async Task<ActionResult<bool>> ValidateSupplierInvoiceNumber([FromQuery] string supplierInvoiceNumber, [FromQuery] int supplierId, [FromQuery] int? excludeFactureId = null)
        {
            try
            {
                var isValid = await _factureService.ValidateSupplierInvoiceNumberAsync(supplierInvoiceNumber, supplierId, excludeFactureId);
                return Ok(new { isValid });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating supplier invoice number {SupplierInvoiceNumber}", supplierInvoiceNumber);
                return StatusCode(500, new { message = "An error occurred while validating invoice number." });
            }
        }

        /// <summary>
        /// Generate internal reference number
        /// </summary>
        [HttpGet("generate-reference")]
        [Authorize(Policy = "Facture.Receive")]
        public async Task<ActionResult<string>> GenerateInternalReferenceNumber()
        {
            try
            {
                var referenceNumber = await _factureService.GenerateInternalReferenceNumberAsync();
                return Ok(new { referenceNumber });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating internal reference number");
                return StatusCode(500, new { message = "An error occurred while generating reference number." });
            }
        }
    }

    // ==================== REQUEST MODELS ==================== //

    public class UpdatePaymentRequest
    {
        public string? Notes { get; set; }
        public string? BankAccount { get; set; }
    }

    public class RecordDeliveryRequest
    {
        public DateTime DeliveryDate { get; set; }
        public string? DeliveryNoteNumber { get; set; }
    }

    public class RequestClarificationRequest
    {
        public string ClarificationReason { get; set; } = string.Empty;
    }
}