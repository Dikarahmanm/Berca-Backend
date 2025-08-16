using Microsoft.EntityFrameworkCore;
using Berca_Backend.Data;
using Berca_Backend.DTOs;
using Berca_Backend.Models;
using Berca_Backend.Services.Interfaces;

namespace Berca_Backend.Services
{
    /// <summary>
    /// Inventory Transfer Service for Toko Eniwan multi-branch management
    /// Handles inventory transfers between branches with full workflow and analytics
    /// </summary>
    public class InventoryTransferService : IInventoryTransferService
    {
        private readonly AppDbContext _context;
        private readonly ITimezoneService _timezoneService;
        private readonly IUserBranchAssignmentService _userBranchService;
        private readonly ILogger<InventoryTransferService> _logger;

        public InventoryTransferService(
            AppDbContext context,
            ITimezoneService timezoneService,
            IUserBranchAssignmentService userBranchService,
            ILogger<InventoryTransferService> logger)
        {
            _context = context;
            _timezoneService = timezoneService;
            _userBranchService = userBranchService;
            _logger = logger;
        }

        // ==================== CORE TRANSFER OPERATIONS ==================== //

        public async Task<InventoryTransferDto> CreateTransferRequestAsync(CreateInventoryTransferRequestDto request, int requestingUserId)
        {
            // Validate transfer request
            var (isValid, validationErrors) = await ValidateTransferRequestAsync(request, requestingUserId);
            if (!isValid)
            {
                throw new InvalidOperationException($"Transfer validation failed: {string.Join(", ", validationErrors)}");
            }

            var now = _timezoneService.Now;
            var transferNumber = await GenerateTransferNumberAsync();

            // Calculate transfer cost and distance
            var transferCost = await CalculateTransferCostAsync(request.SourceBranchId, request.DestinationBranchId, request.TransferItems);
            var distance = await CalculateDistanceBetweenBranchesAsync(request.SourceBranchId, request.DestinationBranchId);

            var transfer = new InventoryTransfer
            {
                TransferNumber = transferNumber,
                Status = TransferStatus.Pending,
                Type = request.Type,
                Priority = request.Priority,
                SourceBranchId = request.SourceBranchId,
                DestinationBranchId = request.DestinationBranchId,
                RequestReason = request.RequestReason,
                Notes = request.Notes,
                EstimatedCost = request.EstimatedCost > 0 ? request.EstimatedCost : transferCost,
                DistanceKm = distance,
                RequestedBy = requestingUserId,
                EstimatedDeliveryDate = request.EstimatedDeliveryDate ?? await EstimateDeliveryDateAsync(request.SourceBranchId, request.DestinationBranchId, request.Priority),
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.InventoryTransfers.Add(transfer);
            await _context.SaveChangesAsync();

            // Create transfer items
            foreach (var itemDto in request.TransferItems)
            {
                var product = await _context.Products.FindAsync(itemDto.ProductId);
                if (product == null) continue;

                var transferItem = new InventoryTransferItem
                {
                    InventoryTransferId = transfer.Id,
                    ProductId = itemDto.ProductId,
                    Quantity = itemDto.Quantity,
                    UnitCost = product.BuyPrice,
                    TotalCost = product.BuyPrice * itemDto.Quantity,
                    SourceStockBefore = product.Stock,
                    SourceStockAfter = product.Stock, // Will be updated on approval
                    ExpiryDate = itemDto.ExpiryDate,
                    BatchNumber = itemDto.BatchNumber,
                    QualityNotes = itemDto.QualityNotes
                };

                _context.InventoryTransferItems.Add(transferItem);
            }

            await _context.SaveChangesAsync();

            // Log status change
            await LogTransferStatusChangeAsync(transfer.Id, TransferStatus.Pending, TransferStatus.Pending, requestingUserId, "Transfer request created");

            // Auto-approve emergency transfers under threshold
            if (request.Priority == TransferPriority.Emergency && transfer.TotalValue <= 1000000) // 1M IDR threshold
            {
                var autoApproval = new TransferApprovalRequestDto
                {
                    IsApproved = true,
                    ApprovalNotes = "Auto-approved emergency transfer under threshold"
                };
                return await ApproveTransferAsync(transfer.Id, autoApproval, requestingUserId);
            }

            _logger.LogInformation("Transfer request created: {TransferNumber} by user {UserId}", transferNumber, requestingUserId);

            return await GetTransferByIdAsync(transfer.Id, requestingUserId) ?? throw new InvalidOperationException("Failed to retrieve created transfer");
        }

        public async Task<InventoryTransferDto> CreateBulkTransferRequestAsync(BulkTransferRequestDto request, int requestingUserId)
        {
            var transferItems = request.ProductTransfers.Select(pt => new CreateTransferItemDto
            {
                ProductId = pt.ProductId,
                Quantity = pt.Quantity,
                ExpiryDate = pt.ExpiryDate,
                BatchNumber = pt.BatchNumber
            }).ToList();

            var createRequest = new CreateInventoryTransferRequestDto
            {
                SourceBranchId = request.SourceBranchId,
                DestinationBranchId = request.DestinationBranchId,
                Type = TransferType.Bulk,
                Priority = request.Priority,
                RequestReason = request.RequestReason,
                Notes = request.Notes,
                TransferItems = transferItems
            };

            return await CreateTransferRequestAsync(createRequest, requestingUserId);
        }

        public async Task<InventoryTransferDto?> GetTransferByIdAsync(int transferId, int requestingUserId)
        {
            // Check user access
            if (!await CanUserAccessTransferAsync(transferId, requestingUserId))
            {
                return null;
            }

            var transfer = await _context.InventoryTransfers
                .Include(t => t.SourceBranch)
                .Include(t => t.DestinationBranch)
                .Include(t => t.RequestedByUser)
                .Include(t => t.ApprovedByUser)
                .Include(t => t.ShippedByUser)
                .Include(t => t.ReceivedByUser)
                .Include(t => t.CancelledByUser)
                .Include(t => t.TransferItems)
                    .ThenInclude(ti => ti.Product)
                .FirstOrDefaultAsync(t => t.Id == transferId);

            if (transfer == null) return null;

            return MapToTransferDto(transfer);
        }

        public async Task<(List<InventoryTransferSummaryDto> Transfers, int TotalCount)> GetTransfersAsync(InventoryTransferQueryParams queryParams, int requestingUserId)
        {
            var accessibleTransferIds = await GetAccessibleTransferIdsForUserAsync(requestingUserId);

            var query = _context.InventoryTransfers
                .Include(t => t.SourceBranch)
                .Include(t => t.DestinationBranch)
                .Include(t => t.RequestedByUser)
                .Include(t => t.TransferItems)
                .Where(t => accessibleTransferIds.Contains(t.Id));

            // Apply filters
            if (queryParams.SourceBranchId.HasValue)
                query = query.Where(t => t.SourceBranchId == queryParams.SourceBranchId.Value);

            if (queryParams.DestinationBranchId.HasValue)
                query = query.Where(t => t.DestinationBranchId == queryParams.DestinationBranchId.Value);

            if (queryParams.Status.HasValue)
                query = query.Where(t => t.Status == queryParams.Status.Value);

            if (queryParams.Type.HasValue)
                query = query.Where(t => t.Type == queryParams.Type.Value);

            if (queryParams.Priority.HasValue)
                query = query.Where(t => t.Priority == queryParams.Priority.Value);

            if (queryParams.StartDate.HasValue)
                query = query.Where(t => t.CreatedAt >= queryParams.StartDate.Value);

            if (queryParams.EndDate.HasValue)
                query = query.Where(t => t.CreatedAt <= queryParams.EndDate.Value);

            if (!string.IsNullOrEmpty(queryParams.SearchTerm))
            {
                var searchTerm = queryParams.SearchTerm.ToLower();
                query = query.Where(t => 
                    t.TransferNumber.ToLower().Contains(searchTerm) ||
                    t.RequestReason.ToLower().Contains(searchTerm) ||
                    t.SourceBranch.BranchName.ToLower().Contains(searchTerm) ||
                    t.DestinationBranch.BranchName.ToLower().Contains(searchTerm));
            }

            var totalCount = await query.CountAsync();

            // Apply sorting
            query = queryParams.SortBy.ToLower() switch
            {
                "transfernumber" => queryParams.SortOrder.ToLower() == "asc" 
                    ? query.OrderBy(t => t.TransferNumber)
                    : query.OrderByDescending(t => t.TransferNumber),
                "status" => queryParams.SortOrder.ToLower() == "asc"
                    ? query.OrderBy(t => t.Status)
                    : query.OrderByDescending(t => t.Status),
                "totalvalue" => queryParams.SortOrder.ToLower() == "asc"
                    ? query.OrderBy(t => t.TransferItems.Sum(ti => ti.TotalCost))
                    : query.OrderByDescending(t => t.TransferItems.Sum(ti => ti.TotalCost)),
                "estimateddeliverydate" => queryParams.SortOrder.ToLower() == "asc"
                    ? query.OrderBy(t => t.EstimatedDeliveryDate)
                    : query.OrderByDescending(t => t.EstimatedDeliveryDate),
                _ => queryParams.SortOrder.ToLower() == "asc"
                    ? query.OrderBy(t => t.CreatedAt)
                    : query.OrderByDescending(t => t.CreatedAt)
            };

            // Apply pagination
            var transfers = await query
                .Skip((queryParams.Page - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .ToListAsync();

            var transferSummaries = transfers.Select(MapToTransferSummaryDto).ToList();

            return (transferSummaries, totalCount);
        }

        public async Task<List<int>> GetAccessibleTransferIdsForUserAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return new List<int>();

            // Admin can access all transfers
            if (user.Role == "Admin")
            {
                return await _context.InventoryTransfers.Select(t => t.Id).ToListAsync();
            }

            // Get user's accessible branches
            var accessibleBranches = await _userBranchService.GetAccessibleBranchesForUserAsync(userId);
            var accessibleBranchIds = accessibleBranches.Select(b => b.BranchId).ToList();

            // Return transfers where user has access to source or destination branch
            return await _context.InventoryTransfers
                .Where(t => accessibleBranchIds.Contains(t.SourceBranchId) || 
                           accessibleBranchIds.Contains(t.DestinationBranchId))
                .Select(t => t.Id)
                .ToListAsync();
        }

        // ==================== TRANSFER WORKFLOW ==================== //

        public async Task<InventoryTransferDto> ApproveTransferAsync(int transferId, TransferApprovalRequestDto approval, int approvingUserId)
        {
            var transfer = await _context.InventoryTransfers
                .Include(t => t.TransferItems)
                .ThenInclude(ti => ti.Product)
                .FirstOrDefaultAsync(t => t.Id == transferId);

            if (transfer == null)
                throw new ArgumentException("Transfer not found");

            if (transfer.Status != TransferStatus.Pending)
                throw new InvalidOperationException("Transfer is not in pending status");

            if (!await CanUserApproveTransferAsync(transferId, approvingUserId))
                throw new UnauthorizedAccessException("User not authorized to approve this transfer");

            var now = _timezoneService.Now;

            if (approval.IsApproved)
            {
                // Validate stock availability at approval time
                foreach (var item in transfer.TransferItems)
                {
                    if (item.Product.Stock < item.Quantity)
                    {
                        throw new InvalidOperationException($"Insufficient stock for product {item.Product.Name}. Available: {item.Product.Stock}, Required: {item.Quantity}");
                    }
                }

                transfer.Status = TransferStatus.Approved;
                transfer.ApprovedBy = approvingUserId;
                transfer.ApprovedAt = now;

                if (approval.AdjustedCost.HasValue)
                    transfer.EstimatedCost = approval.AdjustedCost.Value;

                if (approval.AdjustedDeliveryDate.HasValue)
                    transfer.EstimatedDeliveryDate = approval.AdjustedDeliveryDate.Value;

                // Reserve stock for approved transfer
                await ReserveStockForTransferAsync(transferId);

                _logger.LogInformation("Transfer approved: {TransferNumber} by user {UserId}", transfer.TransferNumber, approvingUserId);
            }
            else
            {
                transfer.Status = TransferStatus.Rejected;
                transfer.CancelledBy = approvingUserId;
                transfer.CancelledAt = now;
                transfer.CancellationReason = approval.ApprovalNotes ?? "Transfer rejected";

                _logger.LogInformation("Transfer rejected: {TransferNumber} by user {UserId}", transfer.TransferNumber, approvingUserId);
            }

            transfer.UpdatedAt = now;
            await _context.SaveChangesAsync();

            // Log status change
            var newStatus = approval.IsApproved ? TransferStatus.Approved : TransferStatus.Rejected;
            await LogTransferStatusChangeAsync(transferId, TransferStatus.Pending, newStatus, approvingUserId, approval.ApprovalNotes);

            return await GetTransferByIdAsync(transferId, approvingUserId) ?? throw new InvalidOperationException("Failed to retrieve approved transfer");
        }

        public async Task<InventoryTransferDto> ShipTransferAsync(int transferId, TransferShipmentRequestDto shipment, int shippingUserId)
        {
            var transfer = await _context.InventoryTransfers.FindAsync(transferId);
            if (transfer == null)
                throw new ArgumentException("Transfer not found");

            if (transfer.Status != TransferStatus.Approved)
                throw new InvalidOperationException("Transfer must be approved before shipping");

            if (!await CanUserAccessTransferAsync(transferId, shippingUserId))
                throw new UnauthorizedAccessException("User not authorized to ship this transfer");

            var now = _timezoneService.Now;

            transfer.Status = TransferStatus.InTransit;
            transfer.ShippedBy = shippingUserId;
            transfer.ShippedAt = now;
            transfer.LogisticsProvider = shipment.LogisticsProvider;
            transfer.TrackingNumber = shipment.TrackingNumber;
            transfer.UpdatedAt = now;

            if (shipment.EstimatedDeliveryDate.HasValue)
                transfer.EstimatedDeliveryDate = shipment.EstimatedDeliveryDate.Value;

            if (shipment.ActualCost.HasValue)
                transfer.ActualCost = shipment.ActualCost.Value;

            await _context.SaveChangesAsync();

            // Create inventory mutations for source branch (stock out)
            await CreateInventoryMutationsForTransferAsync(transferId, MutationType.Transfer);

            // Log status change
            await LogTransferStatusChangeAsync(transferId, TransferStatus.Approved, TransferStatus.InTransit, shippingUserId, shipment.ShippingNotes);

            _logger.LogInformation("Transfer shipped: {TransferNumber} by user {UserId}", transfer.TransferNumber, shippingUserId);

            return await GetTransferByIdAsync(transferId, shippingUserId) ?? throw new InvalidOperationException("Failed to retrieve shipped transfer");
        }

        public async Task<InventoryTransferDto> ReceiveTransferAsync(int transferId, TransferReceiptRequestDto receipt, int receivingUserId)
        {
            var transfer = await _context.InventoryTransfers
                .Include(t => t.TransferItems)
                .ThenInclude(ti => ti.Product)
                .FirstOrDefaultAsync(t => t.Id == transferId);

            if (transfer == null)
                throw new ArgumentException("Transfer not found");

            if (transfer.Status != TransferStatus.InTransit)
                throw new InvalidOperationException("Transfer must be in transit before receiving");

            if (!await CanUserAccessTransferAsync(transferId, receivingUserId))
                throw new UnauthorizedAccessException("User not authorized to receive this transfer");

            var now = _timezoneService.Now;

            // Process received items
            foreach (var receiptItem in receipt.ReceivedItems)
            {
                var transferItem = transfer.TransferItems.FirstOrDefault(ti => ti.Id == receiptItem.TransferItemId);
                if (transferItem == null) continue;

                // Update destination stock levels
                transferItem.DestinationStockBefore = transferItem.Product.Stock;
                transferItem.DestinationStockAfter = transferItem.Product.Stock + receiptItem.ReceivedQuantity;

                // Update product stock
                transferItem.Product.Stock += receiptItem.ReceivedQuantity;
                transferItem.Product.UpdatedAt = now;

                // Update quality notes if provided
                if (!string.IsNullOrEmpty(receiptItem.QualityNotes))
                {
                    transferItem.QualityNotes = receiptItem.QualityNotes;
                }
            }

            transfer.Status = TransferStatus.Completed;
            transfer.ReceivedBy = receivingUserId;
            transfer.ReceivedAt = now;
            transfer.UpdatedAt = now;

            await _context.SaveChangesAsync();

            // Update final inventory levels
            await UpdateInventoryOnTransferCompletionAsync(transferId);

            // Log status change
            await LogTransferStatusChangeAsync(transferId, TransferStatus.InTransit, TransferStatus.Completed, receivingUserId, receipt.ReceiptNotes);

            _logger.LogInformation("Transfer completed: {TransferNumber} by user {UserId}", transfer.TransferNumber, receivingUserId);

            return await GetTransferByIdAsync(transferId, receivingUserId) ?? throw new InvalidOperationException("Failed to retrieve completed transfer");
        }

        public async Task<InventoryTransferDto> CancelTransferAsync(int transferId, string cancellationReason, int cancellingUserId)
        {
            var transfer = await _context.InventoryTransfers.FindAsync(transferId);
            if (transfer == null)
                throw new ArgumentException("Transfer not found");

            if (!transfer.CanBeCancelled)
                throw new InvalidOperationException("Transfer cannot be cancelled in its current status");

            if (!await CanUserAccessTransferAsync(transferId, cancellingUserId))
                throw new UnauthorizedAccessException("User not authorized to cancel this transfer");

            var oldStatus = transfer.Status;
            var now = _timezoneService.Now;

            transfer.Status = TransferStatus.Cancelled;
            transfer.CancelledBy = cancellingUserId;
            transfer.CancelledAt = now;
            transfer.CancellationReason = cancellationReason;
            transfer.UpdatedAt = now;

            await _context.SaveChangesAsync();

            // Release reserved stock if transfer was approved
            if (oldStatus == TransferStatus.Approved)
            {
                await ReleaseReservedStockAsync(transferId);
            }

            // Log status change
            await LogTransferStatusChangeAsync(transferId, oldStatus, TransferStatus.Cancelled, cancellingUserId, cancellationReason);

            _logger.LogInformation("Transfer cancelled: {TransferNumber} by user {UserId}", transfer.TransferNumber, cancellingUserId);

            return await GetTransferByIdAsync(transferId, cancellingUserId) ?? throw new InvalidOperationException("Failed to retrieve cancelled transfer");
        }

        // ==================== BUSINESS LOGIC & VALIDATION ==================== //

        public async Task<(bool IsValid, List<string> ValidationErrors)> ValidateTransferRequestAsync(CreateInventoryTransferRequestDto request, int requestingUserId)
        {
            var errors = new List<string>();

            // Validate source and destination branches
            if (request.SourceBranchId == request.DestinationBranchId)
            {
                errors.Add("Source and destination branches cannot be the same");
            }

            var sourceBranch = await _context.Branches.FindAsync(request.SourceBranchId);
            var destinationBranch = await _context.Branches.FindAsync(request.DestinationBranchId);

            if (sourceBranch == null)
                errors.Add("Source branch not found");
            if (destinationBranch == null)
                errors.Add("Destination branch not found");

            if (sourceBranch != null && !sourceBranch.IsActive)
                errors.Add("Source branch is inactive");
            if (destinationBranch != null && !destinationBranch.IsActive)
                errors.Add("Destination branch is inactive");

            // Validate user access to branches
            var userBranches = await _userBranchService.GetAccessibleBranchesForUserAsync(requestingUserId);
            var userBranchIds = userBranches.Select(b => b.BranchId).ToList();

            if (!userBranchIds.Contains(request.SourceBranchId) && !userBranchIds.Contains(request.DestinationBranchId))
            {
                errors.Add("User does not have access to either source or destination branch");
            }

            // Validate transfer items
            if (!request.TransferItems.Any())
            {
                errors.Add("At least one transfer item is required");
            }

            foreach (var item in request.TransferItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product == null)
                {
                    errors.Add($"Product with ID {item.ProductId} not found");
                    continue;
                }

                if (!product.IsActive)
                {
                    errors.Add($"Product {product.Name} is inactive");
                }

                if (product.Stock < item.Quantity)
                {
                    errors.Add($"Insufficient stock for product {product.Name}. Available: {product.Stock}, Required: {item.Quantity}");
                }

                // Validate expiry date
                if (item.ExpiryDate.HasValue && item.ExpiryDate.Value < DateTime.UtcNow)
                {
                    errors.Add($"Expiry date for product {product.Name} is in the past");
                }
            }

            return (errors.Count == 0, errors);
        }

        public async Task<decimal> CalculateTransferCostAsync(int sourceBranchId, int destinationBranchId, List<CreateTransferItemDto> items)
        {
            var distance = await CalculateDistanceBetweenBranchesAsync(sourceBranchId, destinationBranchId);
            var totalWeight = items.Sum(i => i.Quantity * 0.5m); // Assume 0.5 kg per item average
            
            // Base cost calculation: distance * weight * rate
            var baseCost = distance * totalWeight * 2000; // 2000 IDR per km per kg
            var minCost = 50000; // Minimum 50k IDR
            
            return Math.Max(baseCost, minCost);
        }

        public async Task<decimal> CalculateDistanceBetweenBranchesAsync(int sourceBranchId, int destinationBranchId)
        {
            // Simplified distance calculation - in production would use actual coordinates
            var sourceBranch = await _context.Branches.FindAsync(sourceBranchId);
            var destinationBranch = await _context.Branches.FindAsync(destinationBranchId);
            
            if (sourceBranch == null || destinationBranch == null)
                return 0;

            // Simple distance calculation based on provinces/cities
            return (sourceBranch.Province, destinationBranch.Province) switch
            {
                var (src, dest) when src == dest => 25, // Same province
                ("DKI Jakarta", "Jawa Barat") or ("Jawa Barat", "DKI Jakarta") => 50,
                ("Jawa Barat", "Jawa Timur") or ("Jawa Timur", "Jawa Barat") => 150,
                ("DKI Jakarta", "Jawa Timur") or ("Jawa Timur", "DKI Jakarta") => 180,
                _ => 200 // Default for other combinations
            };
        }

        public async Task<bool> CanUserApproveTransferAsync(int transferId, int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            // Admin can approve all transfers
            if (user.Role == "Admin") return true;

            var transfer = await _context.InventoryTransfers.FindAsync(transferId);
            if (transfer == null) return false;

            // HeadManager can approve high-value transfers
            if (user.Role == "HeadManager" && transfer.RequiresManagerApproval)
                return true;

            // BranchManager can approve transfers for their branch (low value)
            if (user.Role == "BranchManager" && !transfer.RequiresManagerApproval)
            {
                var userBranches = await _userBranchService.GetAccessibleBranchesForUserAsync(userId);
                var userBranchIds = userBranches.Select(b => b.BranchId).ToList();
                return userBranchIds.Contains(transfer.SourceBranchId) || userBranchIds.Contains(transfer.DestinationBranchId);
            }

            return false;
        }

        public async Task<bool> RequiresManagerApprovalAsync(int transferId)
        {
            var transfer = await _context.InventoryTransfers
                .Include(t => t.TransferItems)
                .FirstOrDefaultAsync(t => t.Id == transferId);
            
            return transfer?.RequiresManagerApproval ?? false;
        }

        // ==================== UTILITY METHODS ==================== //

        public async Task<string> GenerateTransferNumberAsync()
        {
            var date = _timezoneService.Now;
            var dateString = date.ToString("yyyyMMdd");
            
            var lastTransfer = await _context.InventoryTransfers
                .Where(t => t.TransferNumber.StartsWith($"TF-{dateString}"))
                .OrderByDescending(t => t.TransferNumber)
                .FirstOrDefaultAsync();

            var sequence = 1;
            if (lastTransfer != null)
            {
                var lastSequence = lastTransfer.TransferNumber.Split('-').LastOrDefault();
                if (int.TryParse(lastSequence, out var lastNum))
                {
                    sequence = lastNum + 1;
                }
            }

            return $"TF-{dateString}-{sequence:D4}";
        }

        public async Task<bool> CanUserAccessTransferAsync(int transferId, int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            // Admin can access all transfers
            if (user.Role == "Admin") return true;

            var transfer = await _context.InventoryTransfers.FindAsync(transferId);
            if (transfer == null) return false;

            // Check if user has access to source or destination branch
            var userBranches = await _userBranchService.GetAccessibleBranchesForUserAsync(userId);
            var userBranchIds = userBranches.Select(b => b.BranchId).ToList();

            return userBranchIds.Contains(transfer.SourceBranchId) || userBranchIds.Contains(transfer.DestinationBranchId);
        }

        public async Task<DateTime> EstimateDeliveryDateAsync(int sourceBranchId, int destinationBranchId, TransferPriority priority = TransferPriority.Normal)
        {
            var distance = await CalculateDistanceBetweenBranchesAsync(sourceBranchId, destinationBranchId);
            var now = _timezoneService.Now;

            // Calculate delivery days based on distance and priority
            var baseDays = priority switch
            {
                TransferPriority.Emergency => 1,
                TransferPriority.High => 2,
                TransferPriority.Normal => 3,
                TransferPriority.Low => 5,
                _ => 3
            };

            // Add extra days for long distances
            if (distance > 100) baseDays += 1;
            if (distance > 200) baseDays += 1;

            return now.AddDays(baseDays);
        }

        // ==================== STOCK MANAGEMENT ==================== //

        public async Task ReserveStockForTransferAsync(int transferId)
        {
            var transfer = await _context.InventoryTransfers
                .Include(t => t.TransferItems)
                .ThenInclude(ti => ti.Product)
                .FirstOrDefaultAsync(t => t.Id == transferId);

            if (transfer == null) return;

            foreach (var item in transfer.TransferItems)
            {
                // Update stock levels to reflect reservation
                item.SourceStockAfter = item.Product.Stock - item.Quantity;
                item.Product.Stock -= item.Quantity;
                item.Product.UpdatedAt = _timezoneService.Now;
            }

            await _context.SaveChangesAsync();
        }

        public async Task ReleaseReservedStockAsync(int transferId)
        {
            var transfer = await _context.InventoryTransfers
                .Include(t => t.TransferItems)
                .ThenInclude(ti => ti.Product)
                .FirstOrDefaultAsync(t => t.Id == transferId);

            if (transfer == null) return;

            foreach (var item in transfer.TransferItems)
            {
                // Restore stock levels
                item.Product.Stock += item.Quantity;
                item.Product.UpdatedAt = _timezoneService.Now;
            }

            await _context.SaveChangesAsync();
        }

        public async Task UpdateInventoryOnTransferCompletionAsync(int transferId)
        {
            // Stock is already updated in ReceiveTransferAsync
            // This method can be used for additional business logic
            await Task.CompletedTask;
        }

        public async Task CreateInventoryMutationsForTransferAsync(int transferId, MutationType mutationType)
        {
            var transfer = await _context.InventoryTransfers
                .Include(t => t.TransferItems)
                .ThenInclude(ti => ti.Product)
                .FirstOrDefaultAsync(t => t.Id == transferId);

            if (transfer == null) return;

            foreach (var item in transfer.TransferItems)
            {
                var mutation = new InventoryMutation
                {
                    ProductId = item.ProductId,
                    Type = mutationType,
                    Quantity = -item.Quantity, // Negative for outbound transfer
                    StockBefore = item.SourceStockBefore,
                    StockAfter = item.SourceStockAfter,
                    Notes = $"Transfer {transfer.TransferNumber} to {transfer.DestinationBranch?.BranchName}",
                    ReferenceNumber = transfer.TransferNumber,
                    UnitCost = item.UnitCost,
                    TotalCost = item.TotalCost,
                    CreatedAt = _timezoneService.Now,
                    CreatedBy = transfer.RequestedByUser?.Username
                };

                _context.InventoryMutations.Add(mutation);
            }

            await _context.SaveChangesAsync();
        }

        // ==================== AUDIT & HISTORY ==================== //

        public async Task<List<InventoryTransferStatusHistory>> GetTransferStatusHistoryAsync(int transferId)
        {
            return await _context.InventoryTransferStatusHistories
                .Include(h => h.ChangedByUser)
                .Where(h => h.InventoryTransferId == transferId)
                .OrderByDescending(h => h.ChangedAt)
                .ToListAsync();
        }

        public async Task LogTransferStatusChangeAsync(int transferId, TransferStatus fromStatus, TransferStatus toStatus, int changedBy, string? reason = null)
        {
            var history = new InventoryTransferStatusHistory
            {
                InventoryTransferId = transferId,
                FromStatus = fromStatus,
                ToStatus = toStatus,
                ChangedBy = changedBy,
                Reason = reason,
                ChangedAt = _timezoneService.Now
            };

            _context.InventoryTransferStatusHistories.Add(history);
            await _context.SaveChangesAsync();
        }

        public async Task<object> GetTransferActivitySummaryAsync(int? branchId = null, int? requestingUserId = null)
        {
            var query = _context.InventoryTransfers.AsQueryable();

            if (branchId.HasValue)
            {
                query = query.Where(t => t.SourceBranchId == branchId.Value || t.DestinationBranchId == branchId.Value);
            }
            else if (requestingUserId.HasValue)
            {
                var accessibleTransferIds = await GetAccessibleTransferIdsForUserAsync(requestingUserId.Value);
                query = query.Where(t => accessibleTransferIds.Contains(t.Id));
            }

            var today = _timezoneService.Now.Date;
            var thisWeek = today.AddDays(-7);
            var thisMonth = today.AddDays(-30);

            return new
            {
                TotalTransfers = await query.CountAsync(),
                PendingApproval = await query.CountAsync(t => t.Status == TransferStatus.Pending),
                InTransit = await query.CountAsync(t => t.Status == TransferStatus.InTransit),
                CompletedToday = await query.CountAsync(t => t.Status == TransferStatus.Completed && t.ReceivedAt >= today),
                CompletedThisWeek = await query.CountAsync(t => t.Status == TransferStatus.Completed && t.ReceivedAt >= thisWeek),
                CompletedThisMonth = await query.CountAsync(t => t.Status == TransferStatus.Completed && t.ReceivedAt >= thisMonth),
                EmergencyTransfers = await query.CountAsync(t => t.Priority == TransferPriority.Emergency && t.CreatedAt >= thisMonth)
            };
        }

        // ==================== PLACEHOLDER METHODS (Analytics & Emergency) ==================== //
        // These would be fully implemented with more complex business logic

        public async Task<InventoryTransferDto> CreateEmergencyTransferAsync(int productId, int destinationBranchId, int quantity, int requestingUserId)
        {
            // Find best source branch with available stock
            var sourceBranches = await GetAvailableSourceBranchesAsync(productId, destinationBranchId, quantity);
            var bestSource = sourceBranches.OrderBy(s => s.Distance).FirstOrDefault();

            if (bestSource == null)
                throw new InvalidOperationException("No available source branch found for emergency transfer");

            var emergencyRequest = new CreateInventoryTransferRequestDto
            {
                SourceBranchId = bestSource.BranchId,
                DestinationBranchId = destinationBranchId,
                Type = TransferType.Emergency,
                Priority = TransferPriority.Emergency,
                RequestReason = "Emergency stock replenishment - Critical shortage",
                TransferItems = new List<CreateTransferItemDto>
                {
                    new CreateTransferItemDto
                    {
                        ProductId = productId,
                        Quantity = quantity
                    }
                }
            };

            return await CreateTransferRequestAsync(emergencyRequest, requestingUserId);
        }

        public async Task<List<AvailableSourceDto>> GetAvailableSourceBranchesAsync(int productId, int destinationBranchId, int requiredQuantity)
        {
            // This is a simplified implementation
            var branches = await _context.Branches
                .Where(b => b.Id != destinationBranchId && b.IsActive)
                .ToListAsync();

            var product = await _context.Products.FindAsync(productId);
            if (product == null) return new List<AvailableSourceDto>();

            return branches.Where(b => product.Stock >= requiredQuantity)
                .Select(b => new AvailableSourceDto
                {
                    BranchId = b.Id,
                    BranchName = b.BranchName,
                    AvailableStock = product.Stock,
                    Distance = 50, // Simplified
                    EstimatedCost = 100000, // Simplified
                    EstimatedDeliveryDays = 2
                }).ToList();
        }

        // Placeholder implementations for analytics methods
        public async Task<List<EmergencyTransferSuggestionDto>> GetEmergencyTransferSuggestionsAsync(int? branchId = null)
        {
            return await Task.FromResult(new List<EmergencyTransferSuggestionDto>());
        }

        public async Task<List<int>> GetProductsRequiringEmergencyTransferAsync(int branchId)
        {
            return await Task.FromResult(new List<int>());
        }

        public async Task<TransferAnalyticsDto> GetTransferAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null, int? requestingUserId = null)
        {
            return await Task.FromResult(new TransferAnalyticsDto());
        }

        public async Task<TransferSuggestionsDto> GetTransferSuggestionsAsync(int? requestingUserId = null)
        {
            return await Task.FromResult(new TransferSuggestionsDto());
        }

        public async Task<List<BranchTransferStatsDto>> GetBranchTransferStatsAsync(DateTime? startDate = null, DateTime? endDate = null, int? requestingUserId = null)
        {
            return await Task.FromResult(new List<BranchTransferStatsDto>());
        }

        public async Task<List<TransferTrendDto>> GetTransferTrendsAsync(DateTime startDate, DateTime endDate, int? requestingUserId = null)
        {
            return await Task.FromResult(new List<TransferTrendDto>());
        }

        public async Task<List<StockRebalancingSuggestionDto>> GetStockRebalancingSuggestionsAsync(int? requestingUserId = null)
        {
            return await Task.FromResult(new List<StockRebalancingSuggestionDto>());
        }

        public async Task<List<TransferEfficiencyDto>> GetRouteOptimizationSuggestionsAsync(int? requestingUserId = null)
        {
            return await Task.FromResult(new List<TransferEfficiencyDto>());
        }

        public async Task<List<InventoryTransferDto>> GenerateAutomaticTransferRecommendationsAsync()
        {
            return await Task.FromResult(new List<InventoryTransferDto>());
        }

        // ==================== HELPER METHODS ==================== //

        private InventoryTransferDto MapToTransferDto(InventoryTransfer transfer)
        {
            return new InventoryTransferDto
            {
                Id = transfer.Id,
                TransferNumber = transfer.TransferNumber,
                Status = transfer.Status,
                StatusDisplay = transfer.StatusDisplay,
                Type = transfer.Type,
                TypeDisplay = transfer.TypeDisplay,
                Priority = transfer.Priority,
                PriorityDisplay = transfer.PriorityDisplay,
                SourceBranch = new BranchSummaryDto
                {
                    Id = transfer.SourceBranch.Id,
                    BranchCode = transfer.SourceBranch.BranchCode,
                    BranchName = transfer.SourceBranch.BranchName,
                    City = transfer.SourceBranch.City,
                    Province = transfer.SourceBranch.Province,
                    ManagerName = transfer.SourceBranch.ManagerName,
                    Phone = transfer.SourceBranch.Phone
                },
                DestinationBranch = new BranchSummaryDto
                {
                    Id = transfer.DestinationBranch.Id,
                    BranchCode = transfer.DestinationBranch.BranchCode,
                    BranchName = transfer.DestinationBranch.BranchName,
                    City = transfer.DestinationBranch.City,
                    Province = transfer.DestinationBranch.Province,
                    ManagerName = transfer.DestinationBranch.ManagerName,
                    Phone = transfer.DestinationBranch.Phone
                },
                RequestReason = transfer.RequestReason,
                Notes = transfer.Notes,
                EstimatedCost = transfer.EstimatedCost,
                ActualCost = transfer.ActualCost,
                DistanceKm = transfer.DistanceKm,
                TransferItems = transfer.TransferItems.Select(MapToTransferItemDto).ToList(),
                RequestedBy = new UserSummaryDto
                {
                    Id = transfer.RequestedByUser.Id,
                    Username = transfer.RequestedByUser.Username,
                    Role = transfer.RequestedByUser.Role
                },
                ApprovedBy = transfer.ApprovedByUser != null ? new UserSummaryDto
                {
                    Id = transfer.ApprovedByUser.Id,
                    Username = transfer.ApprovedByUser.Username,
                    Role = transfer.ApprovedByUser.Role
                } : null,
                ApprovedAt = transfer.ApprovedAt,
                ShippedBy = transfer.ShippedByUser != null ? new UserSummaryDto
                {
                    Id = transfer.ShippedByUser.Id,
                    Username = transfer.ShippedByUser.Username,
                    Role = transfer.ShippedByUser.Role
                } : null,
                ShippedAt = transfer.ShippedAt,
                ReceivedBy = transfer.ReceivedByUser != null ? new UserSummaryDto
                {
                    Id = transfer.ReceivedByUser.Id,
                    Username = transfer.ReceivedByUser.Username,
                    Role = transfer.ReceivedByUser.Role
                } : null,
                ReceivedAt = transfer.ReceivedAt,
                CancelledBy = transfer.CancelledByUser != null ? new UserSummaryDto
                {
                    Id = transfer.CancelledByUser.Id,
                    Username = transfer.CancelledByUser.Username,
                    Role = transfer.CancelledByUser.Role
                } : null,
                CancelledAt = transfer.CancelledAt,
                CancellationReason = transfer.CancellationReason,
                LogisticsProvider = transfer.LogisticsProvider,
                TrackingNumber = transfer.TrackingNumber,
                EstimatedDeliveryDate = transfer.EstimatedDeliveryDate,
                TotalItems = transfer.TotalItems,
                TotalValue = transfer.TotalValue,
                RequiresManagerApproval = transfer.RequiresManagerApproval,
                IsEmergencyTransfer = transfer.IsEmergencyTransfer,
                ProcessingTime = transfer.ProcessingTime,
                CreatedAt = transfer.CreatedAt,
                UpdatedAt = transfer.UpdatedAt
            };
        }

        private InventoryTransferItemDto MapToTransferItemDto(InventoryTransferItem item)
        {
            return new InventoryTransferItemDto
            {
                Id = item.Id,
                Product = new ProductSummaryDto
                {
                    Id = item.Product.Id,
                    Name = item.Product.Name,
                    Barcode = item.Product.Barcode,
                    Unit = item.Product.Unit,
                    SellPrice = item.Product.SellPrice,
                    CurrentStock = item.Product.Stock,
                    MinimumStock = item.Product.MinimumStock,
                    IsLowStock = item.Product.IsLowStock
                },
                Quantity = item.Quantity,
                UnitCost = item.UnitCost,
                TotalCost = item.TotalCost,
                SourceStockBefore = item.SourceStockBefore,
                SourceStockAfter = item.SourceStockAfter,
                DestinationStockBefore = item.DestinationStockBefore,
                DestinationStockAfter = item.DestinationStockAfter,
                ExpiryDate = item.ExpiryDate,
                BatchNumber = item.BatchNumber,
                QualityNotes = item.QualityNotes,
                IsExpired = item.IsExpired,
                IsNearExpiry = item.IsNearExpiry
            };
        }

        private InventoryTransferSummaryDto MapToTransferSummaryDto(InventoryTransfer transfer)
        {
            return new InventoryTransferSummaryDto
            {
                Id = transfer.Id,
                TransferNumber = transfer.TransferNumber,
                Status = transfer.Status,
                StatusDisplay = transfer.StatusDisplay,
                Type = transfer.Type,
                Priority = transfer.Priority,
                SourceBranchName = transfer.SourceBranch.BranchName,
                DestinationBranchName = transfer.DestinationBranch.BranchName,
                TotalItems = transfer.TotalItems,
                TotalValue = transfer.TotalValue,
                RequestedByName = transfer.RequestedByUser.Username,
                CreatedAt = transfer.CreatedAt,
                EstimatedDeliveryDate = transfer.EstimatedDeliveryDate,
                RequiresManagerApproval = transfer.RequiresManagerApproval,
                IsEmergencyTransfer = transfer.IsEmergencyTransfer
            };
        }
    }
}