using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Berca_Backend.Data;
using Berca_Backend.Models;

namespace Berca_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TransferController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TransferController> _logger;

        public TransferController(AppDbContext context, ILogger<TransferController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get transfer requests with branch filtering
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "Transfer.Read")]
        public async Task<IActionResult> GetTransfers(
            [FromQuery] string? branchIds = null,
            [FromQuery] string? status = null,
            [FromQuery] string? priority = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                var query = _context.TransferRequests
                    .Include(tr => tr.SourceBranch)
                    .Include(tr => tr.TargetBranch)
                    .Include(tr => tr.RequestedByUser)
                    .Include(tr => tr.ApprovedByUser)
                    .Include(tr => tr.Items)
                        .ThenInclude(ti => ti.Product)
                    .AsQueryable();

                // Apply branch filtering
                if (!string.IsNullOrEmpty(branchIds))
                {
                    var branchIdList = branchIds.Split(',')
                        .Where(id => int.TryParse(id.Trim(), out _))
                        .Select(id => int.Parse(id.Trim()))
                        .ToList();

                    query = query.Where(tr => branchIdList.Contains(tr.SourceBranchId) || branchIdList.Contains(tr.TargetBranchId));
                }

                // Apply status filtering
                if (!string.IsNullOrEmpty(status) && Enum.TryParse<TransferStatus>(status, true, out var statusEnum))
                {
                    query = query.Where(tr => tr.Status == statusEnum);
                }

                // Apply priority filtering
                if (!string.IsNullOrEmpty(priority) && Enum.TryParse<TransferPriority>(priority, true, out var priorityEnum))
                {
                    query = query.Where(tr => tr.Priority == priorityEnum);
                }

                // Apply user access filtering (non-admin users)
                if (!IsAdminOrHeadManager(currentUserRole))
                {
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == currentUserId);
                    if (user != null)
                    {
                        var accessibleBranchIds = user.GetAccessibleBranchIds();
                        if (user.BranchId.HasValue)
                        {
                            accessibleBranchIds.Add(user.BranchId.Value);
                        }

                        query = query.Where(tr => accessibleBranchIds.Contains(tr.SourceBranchId) || 
                                                 accessibleBranchIds.Contains(tr.TargetBranchId));
                    }
                }

                // Get total count for pagination
                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                // Apply pagination
                var transfers = await query
                    .OrderByDescending(tr => tr.RequestedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(tr => new
                    {
                        id = tr.Id,
                        transferNumber = tr.TransferNumber,
                        sourceBranchId = tr.SourceBranchId,
                        sourceBranchName = tr.SourceBranch.BranchName,
                        targetBranchId = tr.TargetBranchId,
                        targetBranchName = tr.TargetBranch.BranchName,
                        status = tr.Status.ToString(),
                        priority = tr.Priority.ToString(),
                        reason = tr.Reason,
                        notes = tr.Notes,
                        totalItems = tr.TotalItems,
                        totalValue = tr.TotalValue,
                        requestedBy = tr.RequestedByUser.Username,
                        requestedAt = tr.RequestedAt,
                        approvedBy = tr.ApprovedByUser != null ? tr.ApprovedByUser.Username : null,
                        approvedAt = tr.ApprovedAt,
                        completedAt = tr.CompletedAt,
                        items = tr.Items.Select(ti => new
                        {
                            productId = ti.ProductId,
                            productName = ti.Product.Name,
                            productCode = ti.Product.Barcode,
                            requestedQuantity = ti.RequestedQuantity,
                            approvedQuantity = ti.ApprovedQuantity,
                            transferredQuantity = ti.TransferredQuantity,
                            unitPrice = ti.UnitPrice,
                            totalPrice = ti.TotalPrice,
                            notes = ti.Notes
                        }).ToList()
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        transfers = transfers,
                        totalCount = totalCount,
                        currentPage = page,
                        totalPages = totalPages
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transfer requests");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Create new transfer request
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "Transfer.Create")]
        public async Task<IActionResult> CreateTransfer([FromBody] CreateTransferRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid input data",
                    errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var currentUserId = GetCurrentUserId();

                // Validate branches exist
                var sourceBranch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == request.SourceBranchId && b.IsActive);
                var targetBranch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == request.TargetBranchId && b.IsActive);

                if (sourceBranch == null || targetBranch == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid source or target branch"
                    });
                }

                // Create transfer request
                var transferRequest = new TransferRequest
                {
                    TransferNumber = TransferRequest.GenerateTransferNumber(),
                    SourceBranchId = request.SourceBranchId,
                    TargetBranchId = request.TargetBranchId,
                    Status = TransferStatus.Pending,
                    Priority = Enum.TryParse<TransferPriority>(request.Priority, true, out var priority) ? priority : TransferPriority.Normal,
                    Reason = request.Reason,
                    Notes = request.Notes,
                    RequestedBy = currentUserId,
                    RequestedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.TransferRequests.Add(transferRequest);
                await _context.SaveChangesAsync();

                // Create transfer items
                var transferItems = new List<TransferItem>();
                foreach (var item in request.Items)
                {
                    var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId);
                    if (product == null)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(new
                        {
                            success = false,
                            message = $"Product with ID {item.ProductId} not found"
                        });
                    }

                    var transferItem = TransferItem.Create(
                        item.ProductId,
                        item.RequestedQuantity,
                        product.BuyPrice, // Use buy price as unit price
                        item.Notes
                    );

                    transferItem.TransferRequestId = transferRequest.Id;
                    transferItems.Add(transferItem);
                }

                _context.TransferItems.AddRange(transferItems);
                await _context.SaveChangesAsync();

                // Update total values
                transferRequest.UpdateTotalValues();
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                // Log the creation
                _logger.LogInformation("Transfer request {TransferNumber} created by user {UserId}", 
                    transferRequest.TransferNumber, currentUserId);

                return CreatedAtAction(nameof(GetTransfer), new { id = transferRequest.Id }, new
                {
                    success = true,
                    data = new
                    {
                        id = transferRequest.Id,
                        transferNumber = transferRequest.TransferNumber,
                        status = transferRequest.Status.ToString(),
                        message = "Transfer request created successfully"
                    }
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating transfer request");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get transfer request by ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Policy = "Transfer.Read")]
        public async Task<IActionResult> GetTransfer(int id)
        {
            try
            {
                var transfer = await _context.TransferRequests
                    .Include(tr => tr.SourceBranch)
                    .Include(tr => tr.TargetBranch)
                    .Include(tr => tr.RequestedByUser)
                    .Include(tr => tr.ApprovedByUser)
                    .Include(tr => tr.Items)
                        .ThenInclude(ti => ti.Product)
                    .FirstOrDefaultAsync(tr => tr.Id == id);

                if (transfer == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Transfer request not found"
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = transfer.Id,
                        transferNumber = transfer.TransferNumber,
                        sourceBranchId = transfer.SourceBranchId,
                        sourceBranchName = transfer.SourceBranch.BranchName,
                        targetBranchId = transfer.TargetBranchId,
                        targetBranchName = transfer.TargetBranch.BranchName,
                        status = transfer.Status.ToString(),
                        priority = transfer.Priority.ToString(),
                        reason = transfer.Reason,
                        notes = transfer.Notes,
                        totalItems = transfer.TotalItems,
                        totalValue = transfer.TotalValue,
                        requestedBy = transfer.RequestedByUser.Username,
                        requestedAt = transfer.RequestedAt,
                        approvedBy = transfer.ApprovedByUser?.Username,
                        approvedAt = transfer.ApprovedAt,
                        completedAt = transfer.CompletedAt,
                        items = transfer.Items.Select(ti => new
                        {
                            id = ti.Id,
                            productId = ti.ProductId,
                            productName = ti.Product.Name,
                            productCode = ti.Product.Barcode,
                            requestedQuantity = ti.RequestedQuantity,
                            approvedQuantity = ti.ApprovedQuantity,
                            transferredQuantity = ti.TransferredQuantity,
                            unitPrice = ti.UnitPrice,
                            totalPrice = ti.TotalPrice,
                            notes = ti.Notes,
                            status = ti.StatusText
                        }).ToList()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transfer request {TransferId}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Update transfer status (Approve/Reject/Complete)
        /// </summary>
        [HttpPut("{id}/status")]
        [Authorize(Policy = "Transfer.Approve")]
        public async Task<IActionResult> UpdateTransferStatus(int id, [FromBody] UpdateTransferStatusRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid input data"
                });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var currentUserId = GetCurrentUserId();

                var transfer = await _context.TransferRequests
                    .Include(tr => tr.Items)
                    .FirstOrDefaultAsync(tr => tr.Id == id);

                if (transfer == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Transfer request not found"
                    });
                }

                if (!Enum.TryParse<TransferStatus>(request.Status, true, out var newStatus))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid status"
                    });
                }

                // Apply status change based on new status
                switch (newStatus)
                {
                    case TransferStatus.Approved:
                        transfer.Approve(currentUserId, request.Notes);
                        break;
                    case TransferStatus.Rejected:
                        transfer.Reject(currentUserId, request.Notes ?? "Rejected by manager");
                        break;
                    case TransferStatus.Completed:
                        transfer.Complete();
                        break;
                    default:
                        return BadRequest(new
                        {
                            success = false,
                            message = "Invalid status transition"
                        });
                }

                // Update item quantities if provided
                if (request.Items?.Any() == true)
                {
                    foreach (var itemUpdate in request.Items)
                    {
                        var transferItem = transfer.Items.FirstOrDefault(ti => ti.ProductId == itemUpdate.ProductId);
                        if (transferItem != null && itemUpdate.ApprovedQuantity.HasValue)
                        {
                            transferItem.Approve(itemUpdate.ApprovedQuantity.Value, itemUpdate.Notes);
                        }
                    }
                }

                // Update total values
                transfer.UpdateTotalValues();
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation("Transfer request {TransferNumber} status updated to {Status} by user {UserId}", 
                    transfer.TransferNumber, newStatus, currentUserId);

                return Ok(new
                {
                    success = true,
                    message = $"Transfer request {newStatus.ToString().ToLower()} successfully",
                    data = new
                    {
                        id = transfer.Id,
                        status = transfer.Status.ToString(),
                        updatedAt = transfer.UpdatedAt
                    }
                });
            }
            catch (InvalidOperationException ex)
            {
                await transaction.RollbackAsync();
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating transfer request status for {TransferId}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Mark transfer as completed with actual quantities
        /// </summary>
        [HttpPut("{id}/complete")]
        [Authorize(Policy = "Transfer.Manage")]
        public async Task<IActionResult> CompleteTransfer(int id, [FromBody] CompleteTransferRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var currentUserId = GetCurrentUserId();

                var transfer = await _context.TransferRequests
                    .Include(tr => tr.Items)
                    .FirstOrDefaultAsync(tr => tr.Id == id);

                if (transfer == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Transfer request not found"
                    });
                }

                if (!transfer.CanBeCompleted)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Transfer cannot be completed in current status"
                    });
                }

                // Update transferred quantities
                if (request.Items?.Any() == true)
                {
                    foreach (var itemUpdate in request.Items)
                    {
                        var transferItem = transfer.Items.FirstOrDefault(ti => ti.ProductId == itemUpdate.ProductId);
                        if (transferItem != null)
                        {
                            transferItem.UpdateTransferredQuantity(itemUpdate.TransferredQuantity, itemUpdate.Notes);
                        }
                    }
                }

                // Mark as completed
                transfer.Complete();

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Transfer request {TransferNumber} completed by user {UserId}", 
                    transfer.TransferNumber, currentUserId);

                return Ok(new
                {
                    success = true,
                    message = "Transfer completed successfully",
                    data = new
                    {
                        id = transfer.Id,
                        status = transfer.Status.ToString(),
                        completedAt = transfer.CompletedAt
                    }
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error completing transfer request {TransferId}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error"
                });
            }
        }

        #region Helper Methods

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        private string GetCurrentUserRole()
        {
            return User.FindFirst("Role")?.Value ?? 
                   User.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value ?? 
                   User.FindFirst(ClaimTypes.Role)?.Value ??
                   "User";
        }

        private bool IsAdminOrHeadManager(string role)
        {
            return role.ToUpper() is "ADMIN" or "HEADMANAGER";
        }

        #endregion
    }

    #region Request DTOs

    public class CreateTransferRequest
    {
        public int SourceBranchId { get; set; }
        public int TargetBranchId { get; set; }
        public string Priority { get; set; } = "Normal";
        public string Reason { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public List<CreateTransferItemRequest> Items { get; set; } = new();
    }

    public class CreateTransferItemRequest
    {
        public int ProductId { get; set; }
        public int RequestedQuantity { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateTransferStatusRequest
    {
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public List<UpdateTransferItemRequest>? Items { get; set; }
    }

    public class UpdateTransferItemRequest
    {
        public int ProductId { get; set; }
        public int? ApprovedQuantity { get; set; }
        public string? Notes { get; set; }
    }

    public class CompleteTransferRequest
    {
        public List<CompleteTransferItemRequest>? Items { get; set; }
    }

    public class CompleteTransferItemRequest
    {
        public int ProductId { get; set; }
        public int TransferredQuantity { get; set; }
        public string? Notes { get; set; }
    }

    #endregion
}