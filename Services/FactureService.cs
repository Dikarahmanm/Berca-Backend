using Berca_Backend.Data;
using Berca_Backend.DTOs;
using Berca_Backend.Models;
using Berca_Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Berca_Backend.Services
{
    /// <summary>
    /// Facture Management Service implementation
    /// Handles supplier invoice receiving, verification, and payment workflows
    /// </summary>
    public class FactureService : IFactureService
    {
        private readonly AppDbContext _context;
        private readonly ITimezoneService _timezoneService;
        private readonly ISupplierService _supplierService;
        private readonly ILogger<FactureService> _logger;

        public FactureService(
            AppDbContext context,
            ITimezoneService timezoneService,
            ISupplierService supplierService,
            ILogger<FactureService> logger)
        {
            _context = context;
            _timezoneService = timezoneService;
            _supplierService = supplierService;
            _logger = logger;
        }

        // ==================== FACTURE RECEIVING & WORKFLOW ==================== //

        public async Task<FactureDto> ReceiveSupplierInvoiceAsync(ReceiveFactureDto receiveDto, int receivedByUserId)
        {
            try
            {
                // Validate supplier invoice number uniqueness
                if (!await ValidateSupplierInvoiceNumberAsync(receiveDto.SupplierInvoiceNumber, receiveDto.SupplierId))
                {
                    throw new InvalidOperationException($"Supplier invoice number '{receiveDto.SupplierInvoiceNumber}' already exists for this supplier.");
                }

                // Validate supplier exists and is accessible
                var supplier = await _supplierService.GetSupplierByIdAsync(receiveDto.SupplierId, receivedByUserId);
                if (supplier == null)
                {
                    throw new InvalidOperationException("Supplier not found or not accessible.");
                }

                // Calculate due date if not provided
                var dueDate = receiveDto.DueDate ?? await CalculateDueDateAsync(receiveDto.InvoiceDate, receiveDto.SupplierId);

                // Generate internal reference number
                var internalRefNumber = await GenerateInternalReferenceNumberAsync();

                // Calculate total from items if provided
                var calculatedTotal = receiveDto.Items.Any() 
                    ? CalculateFactureTotals(receiveDto.Items, receiveDto.Tax, receiveDto.Discount)
                    : receiveDto.TotalAmount;

                var facture = new Facture
                {
                    SupplierInvoiceNumber = receiveDto.SupplierInvoiceNumber,
                    InternalReferenceNumber = internalRefNumber,
                    SupplierId = receiveDto.SupplierId,
                    BranchId = receiveDto.BranchId,
                    InvoiceDate = receiveDto.InvoiceDate,
                    DueDate = dueDate,
                    SupplierPONumber = receiveDto.SupplierPONumber,
                    DeliveryDate = receiveDto.DeliveryDate,
                    DeliveryNoteNumber = receiveDto.DeliveryNoteNumber,
                    TotalAmount = calculatedTotal,
                    Tax = receiveDto.Tax,
                    Discount = receiveDto.Discount,
                    Status = FactureStatus.Received,
                    SupplierInvoiceFile = receiveDto.SupplierInvoiceFile,
                    SupportingDocs = receiveDto.SupportingDocs,
                    Description = receiveDto.Description,
                    Notes = receiveDto.Notes,
                    ReceivedBy = receivedByUserId,
                    ReceivedAt = _timezoneService.UtcToLocal(DateTime.UtcNow),
                    CreatedBy = receivedByUserId,
                    CreatedAt = _timezoneService.UtcToLocal(DateTime.UtcNow),
                    UpdatedAt = _timezoneService.UtcToLocal(DateTime.UtcNow)
                };

                _context.Factures.Add(facture);
                await _context.SaveChangesAsync();

                // Add line items if provided
                if (receiveDto.Items.Any())
                {
                    var factureItems = receiveDto.Items.Select(item => new FactureItem
                    {
                        FactureId = facture.Id,
                        ProductId = item.ProductId,
                        SupplierItemCode = item.SupplierItemCode,
                        SupplierItemDescription = item.SupplierItemDescription,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TaxRate = item.TaxRate,
                        DiscountAmount = item.DiscountAmount,
                        Notes = item.Notes,
                        CreatedAt = facture.CreatedAt,
                        UpdatedAt = facture.UpdatedAt
                    }).ToList();

                    _context.FactureItems.AddRange(factureItems);
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Supplier invoice {SupplierInvoiceNumber} received from supplier {SupplierId} by user {UserId}", 
                    facture.SupplierInvoiceNumber, facture.SupplierId, receivedByUserId);

                // Reload with navigation properties
                return await GetFactureByIdAsync(facture.Id, receivedByUserId) 
                    ?? throw new InvalidOperationException("Failed to retrieve created facture.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving supplier invoice for user {UserId}", receivedByUserId);
                throw;
            }
        }

        public async Task<FactureDto?> VerifyFactureItemsAsync(VerifyFactureDto verifyDto, int verifiedByUserId)
        {
            try
            {
                var facture = await _context.Factures
                    .Include(f => f.Items)
                    .FirstOrDefaultAsync(f => f.Id == verifyDto.FactureId);

                if (facture == null || !facture.CanVerify)
                    return null;

                // Check branch access
                if (!await HasFactureAccessAsync(facture.Id, verifiedByUserId))
                    return null;

                var now = _timezoneService.UtcToLocal(DateTime.UtcNow);

                // Update item verification details
                foreach (var itemVerify in verifyDto.Items)
                {
                    var item = facture.Items.FirstOrDefault(i => i.Id == itemVerify.ItemId);
                    if (item != null)
                    {
                        item.ReceivedQuantity = itemVerify.ReceivedQuantity;
                        item.AcceptedQuantity = itemVerify.AcceptedQuantity;
                        item.VerificationNotes = itemVerify.VerificationNotes;
                        item.IsVerified = itemVerify.IsVerified;
                        item.VerifiedAt = now;
                        item.VerifiedBy = verifiedByUserId;
                        item.UpdatedAt = now;
                    }
                }

                // Update facture status and verification info
                facture.Status = FactureStatus.Verified;
                facture.VerifiedBy = verifiedByUserId;
                facture.VerifiedAt = now;
                facture.UpdatedBy = verifiedByUserId;
                facture.UpdatedAt = now;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Facture {FactureId} items verified by user {UserId}", 
                    facture.Id, verifiedByUserId);

                return await GetFactureByIdAsync(facture.Id, verifiedByUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying facture {FactureId} for user {UserId}", 
                    verifyDto.FactureId, verifiedByUserId);
                throw;
            }
        }

        public async Task<FactureDto?> ApproveFactureAsync(int factureId, int approvedByUserId, string? approvalNotes = null)
        {
            try
            {
                var facture = await _context.Factures.FindAsync(factureId);
                if (facture == null || !facture.CanApprove)
                    return null;

                // Check branch access
                if (!await HasFactureAccessAsync(factureId, approvedByUserId))
                    return null;

                var now = _timezoneService.UtcToLocal(DateTime.UtcNow);

                facture.Status = FactureStatus.Approved;
                facture.ApprovedBy = approvedByUserId;
                facture.ApprovedAt = now;
                facture.UpdatedBy = approvedByUserId;
                facture.UpdatedAt = now;

                if (!string.IsNullOrEmpty(approvalNotes))
                {
                    facture.Notes = string.IsNullOrEmpty(facture.Notes) 
                        ? $"Approval: {approvalNotes}"
                        : $"{facture.Notes}\nApproval: {approvalNotes}";
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Facture {FactureId} approved by user {UserId}", 
                    factureId, approvedByUserId);

                return await GetFactureByIdAsync(factureId, approvedByUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving facture {FactureId} for user {UserId}", 
                    factureId, approvedByUserId);
                throw;
            }
        }

        public async Task<FactureDto?> DisputeFactureAsync(DisputeFactureDto disputeDto, int disputedByUserId)
        {
            try
            {
                var facture = await _context.Factures.FindAsync(disputeDto.FactureId);
                if (facture == null || !facture.CanDispute)
                    return null;

                // Check branch access
                if (!await HasFactureAccessAsync(disputeDto.FactureId, disputedByUserId))
                    return null;

                var now = _timezoneService.UtcToLocal(DateTime.UtcNow);

                facture.Status = FactureStatus.Disputed;
                facture.DisputeReason = disputeDto.DisputeReason;
                facture.UpdatedBy = disputedByUserId;
                facture.UpdatedAt = now;

                if (!string.IsNullOrEmpty(disputeDto.AdditionalNotes))
                {
                    facture.Notes = string.IsNullOrEmpty(facture.Notes)
                        ? $"Dispute: {disputeDto.AdditionalNotes}"
                        : $"{facture.Notes}\nDispute: {disputeDto.AdditionalNotes}";
                }

                if (!string.IsNullOrEmpty(disputeDto.SupportingDocuments))
                {
                    facture.SupportingDocs = disputeDto.SupportingDocuments;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Facture {FactureId} disputed by user {UserId}. Reason: {Reason}", 
                    disputeDto.FactureId, disputedByUserId, disputeDto.DisputeReason);

                return await GetFactureByIdAsync(disputeDto.FactureId, disputedByUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disputing facture {FactureId} for user {UserId}", 
                    disputeDto.FactureId, disputedByUserId);
                throw;
            }
        }

        public async Task<bool> CancelFactureAsync(int factureId, int cancelledByUserId, string? cancellationReason = null)
        {
            try
            {
                var facture = await _context.Factures.FindAsync(factureId);
                if (facture == null || !facture.CanCancel)
                    return false;

                // Check branch access
                if (!await HasFactureAccessAsync(factureId, cancelledByUserId))
                    return false;

                var now = _timezoneService.UtcToLocal(DateTime.UtcNow);

                facture.Status = FactureStatus.Cancelled;
                facture.UpdatedBy = cancelledByUserId;
                facture.UpdatedAt = now;

                if (!string.IsNullOrEmpty(cancellationReason))
                {
                    facture.Notes = string.IsNullOrEmpty(facture.Notes)
                        ? $"Cancelled: {cancellationReason}"
                        : $"{facture.Notes}\nCancelled: {cancellationReason}";
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Facture {FactureId} cancelled by user {UserId}. Reason: {Reason}",
                    factureId, cancelledByUserId, cancellationReason ?? "Not specified");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling facture {FactureId} for user {UserId}", 
                    factureId, cancelledByUserId);
                throw;
            }
        }

        // ==================== FACTURE CRUD OPERATIONS ==================== //

        public async Task<FacturePagedResponseDto> GetFacturesAsync(FactureQueryParams queryParams, int requestingUserId)
        {
            try
            {
                // Get user's accessible branches
                var user = await _context.Users.FindAsync(requestingUserId);
                var accessibleBranchIds = GetAccessibleBranchIds(user);

                var query = _context.Factures
                    .Include(f => f.Supplier)
                    .Include(f => f.Branch)
                    .Include(f => f.CreatedByUser)
                    .AsQueryable();

                // Apply branch access control
                if (accessibleBranchIds != null)
                {
                    query = query.Where(f => f.BranchId == null || accessibleBranchIds.Contains(f.BranchId.Value));
                }

                // Apply filters
                if (!string.IsNullOrEmpty(queryParams.Search))
                {
                    var searchLower = queryParams.Search.ToLower();
                    query = query.Where(f =>
                        f.SupplierInvoiceNumber.ToLower().Contains(searchLower) ||
                        f.InternalReferenceNumber.ToLower().Contains(searchLower) ||
                        f.Supplier!.CompanyName.ToLower().Contains(searchLower) ||
                        f.Description!.ToLower().Contains(searchLower));
                }

                if (!string.IsNullOrEmpty(queryParams.SupplierInvoiceNumber))
                {
                    query = query.Where(f => f.SupplierInvoiceNumber.Contains(queryParams.SupplierInvoiceNumber));
                }

                if (!string.IsNullOrEmpty(queryParams.InternalReferenceNumber))
                {
                    query = query.Where(f => f.InternalReferenceNumber.Contains(queryParams.InternalReferenceNumber));
                }

                if (!string.IsNullOrEmpty(queryParams.SupplierPONumber))
                {
                    query = query.Where(f => f.SupplierPONumber!.Contains(queryParams.SupplierPONumber));
                }

                if (queryParams.SupplierId.HasValue)
                {
                    query = query.Where(f => f.SupplierId == queryParams.SupplierId.Value);
                }

                if (queryParams.BranchId.HasValue)
                {
                    query = query.Where(f => f.BranchId == queryParams.BranchId.Value);
                }

                if (queryParams.Status.HasValue)
                {
                    query = query.Where(f => f.Status == queryParams.Status.Value);
                }

                if (queryParams.InvoiceDateFrom.HasValue)
                {
                    query = query.Where(f => f.InvoiceDate >= queryParams.InvoiceDateFrom.Value);
                }

                if (queryParams.InvoiceDateTo.HasValue)
                {
                    query = query.Where(f => f.InvoiceDate <= queryParams.InvoiceDateTo.Value);
                }

                if (queryParams.DueDateFrom.HasValue)
                {
                    query = query.Where(f => f.DueDate >= queryParams.DueDateFrom.Value);
                }

                if (queryParams.DueDateTo.HasValue)
                {
                    query = query.Where(f => f.DueDate <= queryParams.DueDateTo.Value);
                }

                if (queryParams.MinAmount.HasValue)
                {
                    query = query.Where(f => f.TotalAmount >= queryParams.MinAmount.Value);
                }

                if (queryParams.MaxAmount.HasValue)
                {
                    query = query.Where(f => f.TotalAmount <= queryParams.MaxAmount.Value);
                }

                if (queryParams.IsOverdue.HasValue && queryParams.IsOverdue.Value)
                {
                    var today = DateTime.UtcNow;
                    query = query.Where(f => f.DueDate < today && f.Status != FactureStatus.Paid && f.Status != FactureStatus.Cancelled);
                }

                if (queryParams.RequiresApproval.HasValue && queryParams.RequiresApproval.Value)
                {
                    query = query.Where(f => f.TotalAmount >= 50000000); // 50M IDR threshold
                }

                if (queryParams.PendingVerification.HasValue && queryParams.PendingVerification.Value)
                {
                    query = query.Where(f => f.Status == FactureStatus.Received);
                }

                // Apply sorting
                query = queryParams.SortBy.ToLower() switch
                {
                    "supplierinvoicenumber" => queryParams.SortOrder.ToLower() == "desc"
                        ? query.OrderByDescending(f => f.SupplierInvoiceNumber)
                        : query.OrderBy(f => f.SupplierInvoiceNumber),
                    "internalreferencenumber" => queryParams.SortOrder.ToLower() == "desc"
                        ? query.OrderByDescending(f => f.InternalReferenceNumber)
                        : query.OrderBy(f => f.InternalReferenceNumber),
                    "suppliername" => queryParams.SortOrder.ToLower() == "desc"
                        ? query.OrderByDescending(f => f.Supplier!.CompanyName)
                        : query.OrderBy(f => f.Supplier!.CompanyName),
                    "duedate" => queryParams.SortOrder.ToLower() == "desc"
                        ? query.OrderByDescending(f => f.DueDate)
                        : query.OrderBy(f => f.DueDate),
                    "totalamount" => queryParams.SortOrder.ToLower() == "desc"
                        ? query.OrderByDescending(f => f.TotalAmount)
                        : query.OrderBy(f => f.TotalAmount),
                    "status" => queryParams.SortOrder.ToLower() == "desc"
                        ? query.OrderByDescending(f => f.Status)
                        : query.OrderBy(f => f.Status),
                    "createdat" => queryParams.SortOrder.ToLower() == "desc"
                        ? query.OrderByDescending(f => f.CreatedAt)
                        : query.OrderBy(f => f.CreatedAt),
                    _ => queryParams.SortOrder.ToLower() == "desc"
                        ? query.OrderByDescending(f => f.InvoiceDate)
                        : query.OrderBy(f => f.InvoiceDate)
                };

                // Get total count
                var totalCount = await query.CountAsync();

                // Apply pagination
                var factures = await query
                    .Skip((queryParams.Page - 1) * queryParams.PageSize)
                    .Take(queryParams.PageSize)
                    .Select(f => new FactureListDto
                    {
                        Id = f.Id,
                        SupplierInvoiceNumber = f.SupplierInvoiceNumber,
                        InternalReferenceNumber = f.InternalReferenceNumber,
                        SupplierName = f.Supplier!.CompanyName,
                        BranchName = f.Branch != null ? f.Branch.BranchName : "Semua Cabang",
                        InvoiceDate = f.InvoiceDate,
                        DueDate = f.DueDate,
                        TotalAmount = f.TotalAmount,
                        OutstandingAmount = f.TotalAmount - f.PaidAmount,
                        Status = f.Status,
                        StatusDisplay = f.StatusDisplay,
                        PaymentPriority = f.PaymentPriority,
                        PriorityDisplay = f.PriorityDisplay,
                        DaysOverdue = f.DaysOverdue,
                        DaysUntilDue = f.DaysUntilDue,
                        IsOverdue = f.IsOverdue,
                        TotalAmountDisplay = f.TotalAmountDisplay,
                        OutstandingAmountDisplay = f.OutstandingAmountDisplay,
                        CreatedAt = f.CreatedAt
                    })
                    .ToListAsync();

                var totalPages = (int)Math.Ceiling((double)totalCount / queryParams.PageSize);

                return new FacturePagedResponseDto
                {
                    Factures = factures,
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
                _logger.LogError(ex, "Error retrieving factures for user {UserId}", requestingUserId);
                throw;
            }
        }

        public async Task<FactureDto?> GetFactureByIdAsync(int factureId, int requestingUserId)
        {
            try
            {
                if (!await HasFactureAccessAsync(factureId, requestingUserId))
                    return null;

                var facture = await _context.Factures
                    .Include(f => f.Supplier)
                    .Include(f => f.Branch)
                    .Include(f => f.CreatedByUser)
                    .Include(f => f.UpdatedByUser)
                    .Include(f => f.ReceivedByUser)
                    .Include(f => f.VerifiedByUser)
                    .Include(f => f.ApprovedByUser)
                    .Include(f => f.Items).ThenInclude(i => i.Product)
                    .Include(f => f.Items).ThenInclude(i => i.VerifiedByUser)
                    .Include(f => f.Payments).ThenInclude(p => p.ProcessedByUser)
                    .Include(f => f.Payments).ThenInclude(p => p.ApprovedByUser)
                    .Include(f => f.Payments).ThenInclude(p => p.ConfirmedByUser)
                    .FirstOrDefaultAsync(f => f.Id == factureId);

                if (facture == null)
                    return null;

                return MapToFactureDto(facture);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving facture {FactureId} for user {UserId}", 
                    factureId, requestingUserId);
                throw;
            }
        }

        public async Task<FactureDto?> GetFactureBySupplierInvoiceNumberAsync(string supplierInvoiceNumber, int supplierId, int requestingUserId)
        {
            try
            {
                var facture = await _context.Factures
                    .Include(f => f.Supplier)
                    .Include(f => f.Branch)
                    .Include(f => f.CreatedByUser)
                    .Include(f => f.UpdatedByUser)
                    .Include(f => f.ReceivedByUser)
                    .Include(f => f.VerifiedByUser)
                    .Include(f => f.ApprovedByUser)
                    .Include(f => f.Items).ThenInclude(i => i.Product)
                    .Include(f => f.Items).ThenInclude(i => i.VerifiedByUser)
                    .Include(f => f.Payments).ThenInclude(p => p.ProcessedByUser)
                    .FirstOrDefaultAsync(f => f.SupplierInvoiceNumber == supplierInvoiceNumber && f.SupplierId == supplierId);

                if (facture == null || !await HasFactureAccessAsync(facture.Id, requestingUserId))
                    return null;

                return MapToFactureDto(facture);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving facture by supplier invoice number {SupplierInvoiceNumber} for supplier {SupplierId}", 
                    supplierInvoiceNumber, supplierId);
                throw;
            }
        }

        public async Task<FactureDto?> UpdateFactureAsync(int factureId, UpdateFactureDto updateDto, int updatedByUserId)
        {
            try
            {
                var facture = await _context.Factures.FindAsync(factureId);
                if (facture == null || !await HasFactureAccessAsync(factureId, updatedByUserId))
                    return null;

                // Only allow updates for received or verified status
                if (facture.Status != FactureStatus.Received && facture.Status != FactureStatus.Verified)
                    return null;

                var now = _timezoneService.UtcToLocal(DateTime.UtcNow);

                if (updateDto.DueDate.HasValue)
                    facture.DueDate = updateDto.DueDate.Value;
                
                if (!string.IsNullOrEmpty(updateDto.SupplierPONumber))
                    facture.SupplierPONumber = updateDto.SupplierPONumber;
                
                if (updateDto.DeliveryDate.HasValue)
                    facture.DeliveryDate = updateDto.DeliveryDate.Value;
                
                if (!string.IsNullOrEmpty(updateDto.DeliveryNoteNumber))
                    facture.DeliveryNoteNumber = updateDto.DeliveryNoteNumber;
                
                if (!string.IsNullOrEmpty(updateDto.Description))
                    facture.Description = updateDto.Description;
                
                if (!string.IsNullOrEmpty(updateDto.Notes))
                    facture.Notes = updateDto.Notes;
                
                if (!string.IsNullOrEmpty(updateDto.ReceiptFile))
                    facture.ReceiptFile = updateDto.ReceiptFile;
                
                if (!string.IsNullOrEmpty(updateDto.SupportingDocs))
                    facture.SupportingDocs = updateDto.SupportingDocs;

                facture.UpdatedBy = updatedByUserId;
                facture.UpdatedAt = now;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Facture {FactureId} updated by user {UserId}", factureId, updatedByUserId);

                return await GetFactureByIdAsync(factureId, updatedByUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating facture {FactureId} for user {UserId}", factureId, updatedByUserId);
                throw;
            }
        }

        // ==================== SUPPLIER-SPECIFIC OPERATIONS ==================== //

        public async Task<List<FactureListDto>> GetSupplierFacturesAsync(int supplierId, int requestingUserId, bool includeCompleted = false, int pageSize = 50)
        {
            try
            {
                var query = _context.Factures
                    .Include(f => f.Supplier)
                    .Include(f => f.Branch)
                    .Where(f => f.SupplierId == supplierId);

                if (!includeCompleted)
                {
                    query = query.Where(f => f.Status != FactureStatus.Paid && f.Status != FactureStatus.Cancelled);
                }

                // Apply branch access control
                var user = await _context.Users.FindAsync(requestingUserId);
                var accessibleBranchIds = GetAccessibleBranchIds(user);
                if (accessibleBranchIds != null)
                {
                    query = query.Where(f => f.BranchId == null || accessibleBranchIds.Contains(f.BranchId.Value));
                }

                var factures = await query
                    .OrderByDescending(f => f.InvoiceDate)
                    .Take(pageSize)
                    .Select(f => new FactureListDto
                    {
                        Id = f.Id,
                        SupplierInvoiceNumber = f.SupplierInvoiceNumber,
                        InternalReferenceNumber = f.InternalReferenceNumber,
                        SupplierName = f.Supplier!.CompanyName,
                        BranchName = f.Branch != null ? f.Branch.BranchName : "Semua Cabang",
                        InvoiceDate = f.InvoiceDate,
                        DueDate = f.DueDate,
                        TotalAmount = f.TotalAmount,
                        OutstandingAmount = f.TotalAmount - f.PaidAmount,
                        Status = f.Status,
                        StatusDisplay = f.StatusDisplay,
                        PaymentPriority = f.PaymentPriority,
                        PriorityDisplay = f.PriorityDisplay,
                        DaysOverdue = f.DaysOverdue,
                        DaysUntilDue = f.DaysUntilDue,
                        IsOverdue = f.IsOverdue,
                        TotalAmountDisplay = f.TotalAmountDisplay,
                        OutstandingAmountDisplay = f.OutstandingAmountDisplay,
                        CreatedAt = f.CreatedAt
                    })
                    .ToListAsync();

                return factures;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving supplier factures for supplier {SupplierId}", supplierId);
                throw;
            }
        }

        public async Task<SupplierFactureSummaryDto?> GetSupplierFactureSummaryAsync(int supplierId, int requestingUserId)
        {
            try
            {
                var supplier = await _supplierService.GetSupplierByIdAsync(supplierId, requestingUserId);
                if (supplier == null)
                    return null;

                var query = _context.Factures.Where(f => f.SupplierId == supplierId);

                // Apply branch access control
                var user = await _context.Users.FindAsync(requestingUserId);
                var accessibleBranchIds = GetAccessibleBranchIds(user);
                if (accessibleBranchIds != null)
                {
                    query = query.Where(f => f.BranchId == null || accessibleBranchIds.Contains(f.BranchId.Value));
                }

                var totalFactures = await query.CountAsync();
                var overdueFactures = await query.CountAsync(f => f.IsOverdue);
                var totalOutstanding = await query.Where(f => f.Status != FactureStatus.Paid && f.Status != FactureStatus.Cancelled)
                    .SumAsync(f => f.TotalAmount - f.PaidAmount);
                var overdueAmount = await query.Where(f => f.IsOverdue)
                    .SumAsync(f => f.TotalAmount - f.PaidAmount);

                var lastPaymentDate = await _context.FacturePayments
                    .Where(p => p.Facture!.SupplierId == supplierId && p.Status == PaymentStatus.Confirmed)
                    .OrderByDescending(p => p.ConfirmedAt)
                    .Select(p => p.ConfirmedAt)
                    .FirstOrDefaultAsync();

                var oldestUnpaidInvoice = await query
                    .Where(f => f.Status != FactureStatus.Paid && f.Status != FactureStatus.Cancelled)
                    .OrderBy(f => f.InvoiceDate)
                    .Select(f => f.InvoiceDate)
                    .FirstOrDefaultAsync();

                var averagePaymentDays = await CalculateAveragePaymentDaysAsync(supplierId);
                var creditLimit = supplier.CreditLimit;
                var creditUtilization = creditLimit > 0 ? (totalOutstanding / creditLimit) * 100 : 0;

                return new SupplierFactureSummaryDto
                {
                    SupplierId = supplierId,
                    SupplierName = supplier.CompanyName,
                    TotalFactures = totalFactures,
                    OverdueFactures = overdueFactures,
                    TotalOutstanding = totalOutstanding,
                    OverdueAmount = overdueAmount,
                    CreditLimit = creditLimit,
                    CreditUtilization = creditUtilization,
                    AveragePaymentDays = averagePaymentDays,
                    LastPaymentDate = lastPaymentDate,
                    OldestUnpaidInvoice = oldestUnpaidInvoice,
                    TotalOutstandingDisplay = totalOutstanding.ToString("C", new CultureInfo("id-ID")),
                    OverdueAmountDisplay = overdueAmount.ToString("C", new CultureInfo("id-ID")),
                    CreditLimitDisplay = creditLimit.ToString("C", new CultureInfo("id-ID")),
                    CreditUtilizationDisplay = $"{creditUtilization:F1}%",
                    IsCreditLimitExceeded = totalOutstanding > creditLimit,
                    HasOverduePayments = overdueAmount > 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving supplier facture summary for supplier {SupplierId}", supplierId);
                throw;
            }
        }

        // ==================== PAYMENT MANAGEMENT ==================== //

        public async Task<FacturePaymentDto> SchedulePaymentAsync(SchedulePaymentDto scheduleDto, int scheduledByUserId)
        {
            try
            {
                var facture = await _context.Factures.FindAsync(scheduleDto.FactureId);
                if (facture == null || !facture.CanSchedulePayment)
                {
                    throw new InvalidOperationException("Facture not found or not available for payment scheduling.");
                }

                if (!await HasFactureAccessAsync(scheduleDto.FactureId, scheduledByUserId))
                {
                    throw new InvalidOperationException("Access denied to this facture.");
                }

                var payment = new FacturePayment
                {
                    FactureId = scheduleDto.FactureId,
                    PaymentDate = scheduleDto.PaymentDate,
                    Amount = scheduleDto.Amount,
                    PaymentMethod = scheduleDto.PaymentMethod,
                    BankAccount = scheduleDto.BankAccount,
                    OurPaymentReference = scheduleDto.OurPaymentReference,
                    Notes = scheduleDto.Notes,
                    IsRecurring = scheduleDto.IsRecurring,
                    RecurrencePattern = scheduleDto.RecurrencePattern,
                    ScheduledDate = scheduleDto.PaymentDate,
                    Status = PaymentStatus.Scheduled,
                    ProcessedBy = scheduledByUserId,
                    CreatedBy = scheduledByUserId,
                    CreatedAt = _timezoneService.UtcToLocal(DateTime.UtcNow),
                    UpdatedAt = _timezoneService.UtcToLocal(DateTime.UtcNow)
                };

                _context.FacturePayments.Add(payment);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Payment scheduled for facture {FactureId} by user {UserId}", 
                    scheduleDto.FactureId, scheduledByUserId);

                return await GetPaymentByIdAsync(payment.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling payment for facture {FactureId}", scheduleDto.FactureId);
                throw;
            }
        }

        public async Task<FacturePaymentDto?> ProcessPaymentAsync(ProcessPaymentDto processDto, int processedByUserId)
        {
            try
            {
                var payment = await _context.FacturePayments
                    .Include(p => p.Facture)
                    .FirstOrDefaultAsync(p => p.Id == processDto.PaymentId);

                if (payment == null || !payment.CanProcess)
                    return null;

                if (!await HasFactureAccessAsync(payment.FactureId, processedByUserId))
                    return null;

                var now = _timezoneService.UtcToLocal(DateTime.UtcNow);

                payment.Status = PaymentStatus.Processed;
                payment.TransferReference = processDto.TransferReference;
                payment.CheckNumber = processDto.CheckNumber;
                payment.PaymentReceiptFile = processDto.PaymentReceiptFile;
                payment.ProcessedBy = processedByUserId;
                payment.UpdatedAt = now;

                if (!string.IsNullOrEmpty(processDto.ProcessingNotes))
                {
                    payment.Notes = string.IsNullOrEmpty(payment.Notes)
                        ? $"Processing: {processDto.ProcessingNotes}"
                        : $"{payment.Notes}\nProcessing: {processDto.ProcessingNotes}";
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Payment {PaymentId} processed by user {UserId}", 
                    processDto.PaymentId, processedByUserId);

                return await GetPaymentByIdAsync(payment.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment {PaymentId}", processDto.PaymentId);
                throw;
            }
        }

        public async Task<FacturePaymentDto?> ConfirmPaymentAsync(ConfirmPaymentDto confirmDto, int confirmedByUserId)
        {
            try
            {
                var payment = await _context.FacturePayments
                    .Include(p => p.Facture)
                    .FirstOrDefaultAsync(p => p.Id == confirmDto.PaymentId);

                if (payment == null || !payment.CanConfirm)
                    return null;

                if (!await HasFactureAccessAsync(payment.FactureId, confirmedByUserId))
                    return null;

                var now = _timezoneService.UtcToLocal(DateTime.UtcNow);

                payment.Status = PaymentStatus.Confirmed;
                payment.SupplierAckReference = confirmDto.SupplierAckReference;
                payment.ConfirmationFile = confirmDto.ConfirmationFile;
                payment.ConfirmedBy = confirmedByUserId;
                payment.ConfirmedAt = now;
                payment.UpdatedAt = now;

                if (!string.IsNullOrEmpty(confirmDto.ConfirmationNotes))
                {
                    payment.Notes = string.IsNullOrEmpty(payment.Notes)
                        ? $"Confirmation: {confirmDto.ConfirmationNotes}"
                        : $"{payment.Notes}\nConfirmation: {confirmDto.ConfirmationNotes}";
                }

                // Update facture paid amount and status
                var facture = payment.Facture!;
                facture.PaidAmount += payment.Amount;
                facture.UpdatedAt = now;
                facture.UpdatedBy = confirmedByUserId;

                if (facture.PaidAmount >= facture.TotalAmount)
                {
                    facture.Status = FactureStatus.Paid;
                }
                else if (facture.PaidAmount > 0)
                {
                    facture.Status = FactureStatus.PartiallyPaid;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Payment {PaymentId} confirmed by user {UserId}", 
                    confirmDto.PaymentId, confirmedByUserId);

                return await GetPaymentByIdAsync(payment.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming payment {PaymentId}", confirmDto.PaymentId);
                throw;
            }
        }

        public async Task<List<FacturePaymentDto>> GetFacturePaymentsAsync(int factureId, int requestingUserId)
        {
            try
            {
                if (!await HasFactureAccessAsync(factureId, requestingUserId))
                    return new List<FacturePaymentDto>();

                var payments = await _context.FacturePayments
                    .Include(p => p.ProcessedByUser)
                    .Include(p => p.ApprovedByUser)
                    .Include(p => p.ConfirmedByUser)
                    .Where(p => p.FactureId == factureId)
                    .OrderBy(p => p.PaymentDate)
                    .ToListAsync();

                return payments.Select(MapToFacturePaymentDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving payments for facture {FactureId}", factureId);
                throw;
            }
        }

        public async Task<FacturePaymentDto?> UpdatePaymentAsync(int paymentId, int updatedByUserId, string? notes = null, string? bankAccount = null)
        {
            try
            {
                var payment = await _context.FacturePayments
                    .Include(p => p.Facture)
                    .FirstOrDefaultAsync(p => p.Id == paymentId);

                if (payment == null || !payment.CanEdit)
                    return null;

                if (!await HasFactureAccessAsync(payment.FactureId, updatedByUserId))
                    return null;

                if (!string.IsNullOrEmpty(notes))
                    payment.Notes = notes;
                
                if (!string.IsNullOrEmpty(bankAccount))
                    payment.BankAccount = bankAccount;

                payment.UpdatedAt = _timezoneService.UtcToLocal(DateTime.UtcNow);

                await _context.SaveChangesAsync();

                _logger.LogInformation("Payment {PaymentId} updated by user {UserId}", paymentId, updatedByUserId);

                return await GetPaymentByIdAsync(paymentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating payment {PaymentId}", paymentId);
                throw;
            }
        }

        public async Task<bool> CancelPaymentAsync(int paymentId, int cancelledByUserId, string? cancellationReason = null)
        {
            try
            {
                var payment = await _context.FacturePayments
                    .Include(p => p.Facture)
                    .FirstOrDefaultAsync(p => p.Id == paymentId);

                if (payment == null || !payment.CanCancel)
                    return false;

                if (!await HasFactureAccessAsync(payment.FactureId, cancelledByUserId))
                    return false;

                var now = _timezoneService.UtcToLocal(DateTime.UtcNow);

                payment.Status = PaymentStatus.Failed;
                payment.FailureReason = cancellationReason ?? "Cancelled by user";
                payment.UpdatedAt = now;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Payment {PaymentId} cancelled by user {UserId}. Reason: {Reason}",
                    paymentId, cancelledByUserId, cancellationReason ?? "Not specified");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling payment {PaymentId}", paymentId);
                throw;
            }
        }

        // ==================== WORKFLOW & STATUS TRACKING ==================== //

        public async Task<List<FactureListDto>> GetFacturesPendingVerificationAsync(int requestingUserId, int? branchId = null)
        {
            try
            {
                var query = _context.Factures
                    .Include(f => f.Supplier)
                    .Include(f => f.Branch)
                    .Where(f => f.Status == FactureStatus.Received);

                // Apply branch filter
                if (branchId.HasValue)
                {
                    query = query.Where(f => f.BranchId == branchId.Value);
                }

                // Apply branch access control
                var user = await _context.Users.FindAsync(requestingUserId);
                var accessibleBranchIds = GetAccessibleBranchIds(user);
                if (accessibleBranchIds != null)
                {
                    query = query.Where(f => f.BranchId == null || accessibleBranchIds.Contains(f.BranchId.Value));
                }

                return await query
                    .OrderBy(f => f.ReceivedAt)
                    .Select(f => new FactureListDto
                    {
                        Id = f.Id,
                        SupplierInvoiceNumber = f.SupplierInvoiceNumber,
                        InternalReferenceNumber = f.InternalReferenceNumber,
                        SupplierName = f.Supplier!.CompanyName,
                        BranchName = f.Branch != null ? f.Branch.BranchName : "Semua Cabang",
                        InvoiceDate = f.InvoiceDate,
                        DueDate = f.DueDate,
                        TotalAmount = f.TotalAmount,
                        OutstandingAmount = f.TotalAmount - f.PaidAmount,
                        Status = f.Status,
                        StatusDisplay = f.StatusDisplay,
                        PaymentPriority = f.PaymentPriority,
                        PriorityDisplay = f.PriorityDisplay,
                        DaysOverdue = f.DaysOverdue,
                        DaysUntilDue = f.DaysUntilDue,
                        IsOverdue = f.IsOverdue,
                        TotalAmountDisplay = f.TotalAmountDisplay,
                        OutstandingAmountDisplay = f.OutstandingAmountDisplay,
                        CreatedAt = f.CreatedAt
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving factures pending verification for user {UserId}", requestingUserId);
                throw;
            }
        }

        public async Task<List<FactureListDto>> GetFacturesPendingApprovalAsync(int requestingUserId, int? branchId = null)
        {
            try
            {
                // Use business logic instead of computed property for approval requirement
                var approvalThreshold = 50000000m; // 50M IDR threshold from business rule
                var query = _context.Factures
                    .Include(f => f.Supplier)
                    .Include(f => f.Branch)
                    .Where(f => f.Status == FactureStatus.Verified && f.TotalAmount >= approvalThreshold);

                // Apply branch filter
                if (branchId.HasValue)
                {
                    query = query.Where(f => f.BranchId == branchId.Value);
                }

                // Apply branch access control
                var user = await _context.Users.FindAsync(requestingUserId);
                var accessibleBranchIds = GetAccessibleBranchIds(user);
                if (accessibleBranchIds != null)
                {
                    query = query.Where(f => f.BranchId == null || accessibleBranchIds.Contains(f.BranchId.Value));
                }

                return await query
                    .OrderBy(f => f.VerifiedAt)
                    .Select(f => new FactureListDto
                    {
                        Id = f.Id,
                        SupplierInvoiceNumber = f.SupplierInvoiceNumber,
                        InternalReferenceNumber = f.InternalReferenceNumber,
                        SupplierName = f.Supplier!.CompanyName,
                        BranchName = f.Branch != null ? f.Branch.BranchName : "Semua Cabang",
                        InvoiceDate = f.InvoiceDate,
                        DueDate = f.DueDate,
                        TotalAmount = f.TotalAmount,
                        OutstandingAmount = f.TotalAmount - f.PaidAmount,
                        Status = f.Status,
                        StatusDisplay = f.StatusDisplay,
                        PaymentPriority = f.PaymentPriority,
                        PriorityDisplay = f.PriorityDisplay,
                        DaysOverdue = f.DaysOverdue,
                        DaysUntilDue = f.DaysUntilDue,
                        IsOverdue = f.IsOverdue,
                        TotalAmountDisplay = f.TotalAmountDisplay,
                        OutstandingAmountDisplay = f.OutstandingAmountDisplay,
                        CreatedAt = f.CreatedAt
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving factures pending approval for user {UserId}", requestingUserId);
                throw;
            }
        }

        public async Task<List<FactureListDto>> GetOverduePaymentsAsync(int requestingUserId, int? branchId = null)
        {
            try
            {
                var query = _context.Factures
                    .Include(f => f.Supplier)
                    .Include(f => f.Branch)
                    .Where(f => f.IsOverdue);

                // Apply branch filter
                if (branchId.HasValue)
                {
                    query = query.Where(f => f.BranchId == branchId.Value);
                }

                // Apply branch access control
                var user = await _context.Users.FindAsync(requestingUserId);
                var accessibleBranchIds = GetAccessibleBranchIds(user);
                if (accessibleBranchIds != null)
                {
                    query = query.Where(f => f.BranchId == null || accessibleBranchIds.Contains(f.BranchId.Value));
                }

                return await query
                    .OrderByDescending(f => f.DaysOverdue)
                    .Select(f => new FactureListDto
                    {
                        Id = f.Id,
                        SupplierInvoiceNumber = f.SupplierInvoiceNumber,
                        InternalReferenceNumber = f.InternalReferenceNumber,
                        SupplierName = f.Supplier!.CompanyName,
                        BranchName = f.Branch != null ? f.Branch.BranchName : "Semua Cabang",
                        InvoiceDate = f.InvoiceDate,
                        DueDate = f.DueDate,
                        TotalAmount = f.TotalAmount,
                        OutstandingAmount = f.TotalAmount - f.PaidAmount,
                        Status = f.Status,
                        StatusDisplay = f.StatusDisplay,
                        PaymentPriority = f.PaymentPriority,
                        PriorityDisplay = f.PriorityDisplay,
                        DaysOverdue = f.DaysOverdue,
                        DaysUntilDue = f.DaysUntilDue,
                        IsOverdue = f.IsOverdue,
                        TotalAmountDisplay = f.TotalAmountDisplay,
                        OutstandingAmountDisplay = f.OutstandingAmountDisplay,
                        CreatedAt = f.CreatedAt
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving overdue payments for user {UserId}", requestingUserId);
                throw;
            }
        }

        public async Task<List<FactureListDto>> GetPaymentsDueSoonAsync(int requestingUserId, int daysAhead = 7, int? branchId = null)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(daysAhead);
                
                var query = _context.Factures
                    .Include(f => f.Supplier)
                    .Include(f => f.Branch)
                    .Where(f => f.Status != FactureStatus.Paid && f.Status != FactureStatus.Cancelled &&
                               f.DueDate <= cutoffDate && f.DueDate > DateTime.UtcNow);

                // Apply branch filter
                if (branchId.HasValue)
                {
                    query = query.Where(f => f.BranchId == branchId.Value);
                }

                // Apply branch access control
                var user = await _context.Users.FindAsync(requestingUserId);
                var accessibleBranchIds = GetAccessibleBranchIds(user);
                if (accessibleBranchIds != null)
                {
                    query = query.Where(f => f.BranchId == null || accessibleBranchIds.Contains(f.BranchId.Value));
                }

                return await query
                    .OrderBy(f => f.DueDate)
                    .Select(f => new FactureListDto
                    {
                        Id = f.Id,
                        SupplierInvoiceNumber = f.SupplierInvoiceNumber,
                        InternalReferenceNumber = f.InternalReferenceNumber,
                        SupplierName = f.Supplier!.CompanyName,
                        BranchName = f.Branch != null ? f.Branch.BranchName : "Semua Cabang",
                        InvoiceDate = f.InvoiceDate,
                        DueDate = f.DueDate,
                        TotalAmount = f.TotalAmount,
                        OutstandingAmount = f.TotalAmount - f.PaidAmount,
                        Status = f.Status,
                        StatusDisplay = f.StatusDisplay,
                        PaymentPriority = f.PaymentPriority,
                        PriorityDisplay = f.PriorityDisplay,
                        DaysOverdue = f.DaysOverdue,
                        DaysUntilDue = f.DaysUntilDue,
                        IsOverdue = f.IsOverdue,
                        TotalAmountDisplay = f.TotalAmountDisplay,
                        OutstandingAmountDisplay = f.OutstandingAmountDisplay,
                        CreatedAt = f.CreatedAt
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving payments due soon for user {UserId}", requestingUserId);
                throw;
            }
        }

        public async Task<List<FactureListDto>> GetPaymentsDueTodayAsync(int requestingUserId, int? branchId = null)
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var tomorrow = today.AddDays(1);
                
                var query = _context.Factures
                    .Include(f => f.Supplier)
                    .Include(f => f.Branch)
                    .Where(f => f.Status != FactureStatus.Paid && f.Status != FactureStatus.Cancelled &&
                               f.DueDate >= today && f.DueDate < tomorrow);

                // Apply branch filter
                if (branchId.HasValue)
                {
                    query = query.Where(f => f.BranchId == branchId.Value);
                }

                // Apply branch access control
                var user = await _context.Users.FindAsync(requestingUserId);
                var accessibleBranchIds = GetAccessibleBranchIds(user);
                if (accessibleBranchIds != null)
                {
                    query = query.Where(f => f.BranchId == null || accessibleBranchIds.Contains(f.BranchId.Value));
                }

                return await query
                    .OrderBy(f => f.DueDate)
                    .Select(f => new FactureListDto
                    {
                        Id = f.Id,
                        SupplierInvoiceNumber = f.SupplierInvoiceNumber,
                        InternalReferenceNumber = f.InternalReferenceNumber,
                        SupplierName = f.Supplier!.CompanyName,
                        BranchName = f.Branch != null ? f.Branch.BranchName : "Semua Cabang",
                        InvoiceDate = f.InvoiceDate,
                        DueDate = f.DueDate,
                        TotalAmount = f.TotalAmount,
                        OutstandingAmount = f.TotalAmount - f.PaidAmount,
                        Status = f.Status,
                        StatusDisplay = f.StatusDisplay,
                        PaymentPriority = f.PaymentPriority,
                        PriorityDisplay = f.PriorityDisplay,
                        DaysOverdue = f.DaysOverdue,
                        DaysUntilDue = f.DaysUntilDue,
                        IsOverdue = f.IsOverdue,
                        TotalAmountDisplay = f.TotalAmountDisplay,
                        OutstandingAmountDisplay = f.OutstandingAmountDisplay,
                        CreatedAt = f.CreatedAt
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving payments due today for user {UserId}", requestingUserId);
                throw;
            }
        }

        // ==================== VALIDATION & BUSINESS LOGIC ==================== //

        public async Task<bool> ValidateSupplierInvoiceNumberAsync(string supplierInvoiceNumber, int supplierId, int? excludeFactureId = null)
        {
            try
            {
                var query = _context.Factures
                    .Where(f => f.SupplierInvoiceNumber == supplierInvoiceNumber && f.SupplierId == supplierId);

                if (excludeFactureId.HasValue)
                {
                    query = query.Where(f => f.Id != excludeFactureId.Value);
                }

                return !await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating supplier invoice number {InvoiceNumber} for supplier {SupplierId}", 
                    supplierInvoiceNumber, supplierId);
                throw;
            }
        }

        public async Task<string> GenerateInternalReferenceNumberAsync()
        {
            try
            {
                var year = DateTime.UtcNow.Year;
                var prefix = $"INT-FAC-{year}-";
                
                var lastSequence = await _context.Factures
                    .Where(f => f.InternalReferenceNumber.StartsWith(prefix))
                    .Select(f => f.InternalReferenceNumber)
                    .OrderByDescending(r => r)
                    .FirstOrDefaultAsync();

                int nextSequence = 1;
                if (!string.IsNullOrEmpty(lastSequence))
                {
                    var sequencePart = lastSequence.Substring(prefix.Length);
                    if (int.TryParse(sequencePart, out int currentSequence))
                    {
                        nextSequence = currentSequence + 1;
                    }
                }

                return $"{prefix}{nextSequence:D5}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating internal reference number");
                throw;
            }
        }

        public async Task<DateTime> CalculateDueDateAsync(DateTime invoiceDate, int supplierId)
        {
            try
            {
                var supplier = await _context.Suppliers.FindAsync(supplierId);
                var paymentTerms = supplier?.PaymentTerms ?? 30;
                
                return invoiceDate.AddDays(paymentTerms);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating due date for supplier {SupplierId}", supplierId);
                throw;
            }
        }

        public decimal CalculateFactureTotals(List<CreateFactureItemDto> items, decimal tax = 0, decimal discount = 0)
        {
            try
            {
                var itemsTotal = items.Sum(item => 
                {
                    var lineTotal = item.Quantity * item.UnitPrice - item.DiscountAmount;
                    var taxAmount = (lineTotal * item.TaxRate) / 100;
                    return lineTotal + taxAmount;
                });

                return itemsTotal + tax - discount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating facture totals");
                throw;
            }
        }

        public async Task<bool> ValidateSupplierCreditLimitAsync(int supplierId, decimal newFactureAmount, int requestingUserId)
        {
            try
            {
                var supplier = await _supplierService.GetSupplierByIdAsync(supplierId, requestingUserId);
                if (supplier == null || supplier.CreditLimit <= 0)
                    return true;

                var currentOutstanding = await _context.Factures
                    .Where(f => f.SupplierId == supplierId && 
                               f.Status != FactureStatus.Paid && 
                               f.Status != FactureStatus.Cancelled)
                    .SumAsync(f => f.TotalAmount - f.PaidAmount);

                return (currentOutstanding + newFactureAmount) <= supplier.CreditLimit;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating supplier credit limit for supplier {SupplierId}", supplierId);
                throw;
            }
        }

        // ==================== ANALYTICS & REPORTING ==================== //

        public async Task<FactureSummaryDto> GetFactureSummaryAsync(int requestingUserId, int? branchId = null)
        {
            try
            {
                var query = _context.Factures.AsQueryable();

                if (branchId.HasValue)
                {
                    query = query.Where(f => f.BranchId == branchId.Value);
                }

                var user = await _context.Users.FindAsync(requestingUserId);
                var accessibleBranchIds = GetAccessibleBranchIds(user);
                if (accessibleBranchIds != null)
                {
                    query = query.Where(f => f.BranchId == null || accessibleBranchIds.Contains(f.BranchId.Value));
                }

                var totalFactures = await query.CountAsync();
                var pendingVerification = await query.CountAsync(f => f.Status == FactureStatus.Received);
                
                // Calculate pending approval based on business logic instead of computed property
                var approvalThreshold = 50000000m; // 50M IDR threshold from business rule
                var pendingApproval = await query.CountAsync(f => f.Status == FactureStatus.Verified && f.TotalAmount >= approvalThreshold);
                
                // Calculate overdue based on date comparison instead of computed property
                var today = DateTime.UtcNow.Date;
                var overduePayments = await query.CountAsync(f => f.Status != FactureStatus.Paid && 
                                                                  f.Status != FactureStatus.Cancelled &&
                                                                  f.DueDate < today);
                var tomorrow = today.AddDays(1);
                var paymentsDueToday = await query.CountAsync(f => 
                    f.Status != FactureStatus.Paid && f.Status != FactureStatus.Cancelled &&
                    f.DueDate >= today && f.DueDate < tomorrow);
                
                var weekFromNow = today.AddDays(7);
                var paymentsDueSoon = await query.CountAsync(f => 
                    f.Status != FactureStatus.Paid && f.Status != FactureStatus.Cancelled &&
                    f.DueDate > today && f.DueDate <= weekFromNow);

                var totalOutstanding = await query
                    .Where(f => f.Status != FactureStatus.Paid && f.Status != FactureStatus.Cancelled)
                    .SumAsync(f => f.TotalAmount - f.PaidAmount);
                
                var totalOverdue = await query
                    .Where(f => f.Status != FactureStatus.Paid && 
                               f.Status != FactureStatus.Cancelled &&
                               f.DueDate < today)
                    .SumAsync(f => f.TotalAmount - f.PaidAmount);
                
                var paymentsThisWeek = await query
                    .Where(f => f.Status != FactureStatus.Paid && f.Status != FactureStatus.Cancelled &&
                               f.DueDate >= today && f.DueDate <= weekFromNow)
                    .SumAsync(f => f.TotalAmount - f.PaidAmount);
                
                var monthFromNow = today.AddDays(30);
                var paymentsThisMonth = await query
                    .Where(f => f.Status != FactureStatus.Paid && f.Status != FactureStatus.Cancelled &&
                               f.DueDate >= today && f.DueDate <= monthFromNow)
                    .SumAsync(f => f.TotalAmount - f.PaidAmount);

                return new FactureSummaryDto
                {
                    TotalFactures = totalFactures,
                    PendingVerification = pendingVerification,
                    PendingApproval = pendingApproval,
                    OverduePayments = overduePayments,
                    PaymentsDueToday = paymentsDueToday,
                    PaymentsDueSoon = paymentsDueSoon,
                    TotalOutstanding = totalOutstanding,
                    TotalOverdue = totalOverdue,
                    PaymentsDueThisWeek = paymentsThisWeek,
                    PaymentsDueThisMonth = paymentsThisMonth,
                    TotalOutstandingDisplay = totalOutstanding.ToString("C", new CultureInfo("id-ID")),
                    TotalOverdueDisplay = totalOverdue.ToString("C", new CultureInfo("id-ID")),
                    PaymentsDueThisWeekDisplay = paymentsThisWeek.ToString("C", new CultureInfo("id-ID")),
                    PaymentsDueThisMonthDisplay = paymentsThisMonth.ToString("C", new CultureInfo("id-ID"))
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving facture summary for user {UserId}", requestingUserId);
                throw;
            }
        }

        public async Task<object> GetPaymentAnalyticsAsync(int requestingUserId, int daysAhead = 30, int? branchId = null)
        {
            try
            {
                var startDate = DateTime.UtcNow.Date;
                var endDate = startDate.AddDays(daysAhead);
                
                var query = _context.Factures
                    .Where(f => f.Status != FactureStatus.Paid && f.Status != FactureStatus.Cancelled &&
                               f.DueDate >= startDate && f.DueDate <= endDate);

                if (branchId.HasValue)
                {
                    query = query.Where(f => f.BranchId == branchId.Value);
                }

                var user = await _context.Users.FindAsync(requestingUserId);
                var accessibleBranchIds = GetAccessibleBranchIds(user);
                if (accessibleBranchIds != null)
                {
                    query = query.Where(f => f.BranchId == null || accessibleBranchIds.Contains(f.BranchId.Value));
                }

                var dailyPayments = await query
                    .GroupBy(f => f.DueDate.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        TotalAmount = g.Sum(f => f.TotalAmount - f.PaidAmount),
                        Count = g.Count()
                    })
                    .OrderBy(x => x.Date)
                    .ToListAsync();

                return new
                {
                    DailyPayments = dailyPayments,
                    TotalProjectedPayments = dailyPayments.Sum(d => d.TotalAmount),
                    AverageDailyPayments = dailyPayments.Any() ? dailyPayments.Average(d => d.TotalAmount) : 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving payment analytics for user {UserId}", requestingUserId);
                throw;
            }
        }

        public async Task<object> GetAgingAnalysisAsync(int requestingUserId, int? branchId = null)
        {
            try
            {
                var query = _context.Factures
                    .Where(f => f.Status != FactureStatus.Paid && f.Status != FactureStatus.Cancelled);

                if (branchId.HasValue)
                {
                    query = query.Where(f => f.BranchId == branchId.Value);
                }

                var user = await _context.Users.FindAsync(requestingUserId);
                var accessibleBranchIds = GetAccessibleBranchIds(user);
                if (accessibleBranchIds != null)
                {
                    query = query.Where(f => f.BranchId == null || accessibleBranchIds.Contains(f.BranchId.Value));
                }

                var factures = await query.ToListAsync();
                var today = DateTime.UtcNow.Date;

                var aging = new
                {
                    Current = factures.Where(f => f.DueDate >= today).Sum(f => f.TotalAmount - f.PaidAmount),
                    Days1_30 = factures.Where(f => f.DueDate < today && f.DueDate >= today.AddDays(-30)).Sum(f => f.TotalAmount - f.PaidAmount),
                    Days31_60 = factures.Where(f => f.DueDate < today.AddDays(-30) && f.DueDate >= today.AddDays(-60)).Sum(f => f.TotalAmount - f.PaidAmount),
                    Days61_90 = factures.Where(f => f.DueDate < today.AddDays(-60) && f.DueDate >= today.AddDays(-90)).Sum(f => f.TotalAmount - f.PaidAmount),
                    Over90Days = factures.Where(f => f.DueDate < today.AddDays(-90)).Sum(f => f.TotalAmount - f.PaidAmount)
                };

                var total = aging.Current + aging.Days1_30 + aging.Days31_60 + aging.Days61_90 + aging.Over90Days;

                return new
                {
                    Aging = aging,
                    Percentages = new
                    {
                        Current = total > 0 ? (aging.Current / total) * 100 : 0,
                        Days1_30 = total > 0 ? (aging.Days1_30 / total) * 100 : 0,
                        Days31_60 = total > 0 ? (aging.Days31_60 / total) * 100 : 0,
                        Days61_90 = total > 0 ? (aging.Days61_90 / total) * 100 : 0,
                        Over90Days = total > 0 ? (aging.Over90Days / total) * 100 : 0
                    },
                    TotalOutstanding = total
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving aging analysis for user {UserId}", requestingUserId);
                throw;
            }
        }

        // ==================== DELIVERY & RECEIVING WORKFLOW ==================== //

        public async Task<FactureDto?> RecordDeliveryAsync(int factureId, DateTime deliveryDate, string? deliveryNoteNumber, int receivedByUserId)
        {
            try
            {
                var facture = await _context.Factures.FindAsync(factureId);
                if (facture == null || !await HasFactureAccessAsync(factureId, receivedByUserId))
                    return null;

                var now = _timezoneService.UtcToLocal(DateTime.UtcNow);

                facture.DeliveryDate = deliveryDate;
                facture.DeliveryNoteNumber = deliveryNoteNumber;
                facture.UpdatedBy = receivedByUserId;
                facture.UpdatedAt = now;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Delivery recorded for facture {FactureId} by user {UserId}", 
                    factureId, receivedByUserId);

                return await GetFactureByIdAsync(factureId, receivedByUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording delivery for facture {FactureId}", factureId);
                throw;
            }
        }

        public async Task<bool> RequestClarificationAsync(int factureId, string clarificationReason, int requestedByUserId)
        {
            try
            {
                var facture = await _context.Factures.FindAsync(factureId);
                if (facture == null || !await HasFactureAccessAsync(factureId, requestedByUserId))
                    return false;

                var now = _timezoneService.UtcToLocal(DateTime.UtcNow);

                facture.Notes = string.IsNullOrEmpty(facture.Notes)
                    ? $"Clarification Requested: {clarificationReason}"
                    : $"{facture.Notes}\nClarification Requested: {clarificationReason}";
                
                facture.UpdatedBy = requestedByUserId;
                facture.UpdatedAt = now;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Clarification requested for facture {FactureId} by user {UserId}. Reason: {Reason}",
                    factureId, requestedByUserId, clarificationReason);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting clarification for facture {FactureId}", factureId);
                throw;
            }
        }

        // ==================== NEW ANALYTICS METHODS ==================== //

        public async Task<List<FactureListDto>> GetOutstandingFacturesAsync(int requestingUserId, int? branchId = null, int? supplierId = null, int limit = 50)
        {
            try
            {
                var user = await _context.Users.FindAsync(requestingUserId);
                var userBranches = user?.CanAccessMultipleBranches == true ? 
                    (user.AccessibleBranchIds?.Split(',').Select(int.Parse).ToList() ?? new List<int> { 0 }) : 
                    new List<int> { user?.BranchId ?? 0 };
                var now = _timezoneService.UtcToLocal(DateTime.UtcNow);

                var query = _context.Factures
                    .Include(f => f.Supplier)
                    .Include(f => f.Branch)
                    .Where(f => f.Status != FactureStatus.Paid && 
                               f.Status != FactureStatus.Cancelled &&
                               (f.TotalAmount - f.PaidAmount) > 0);

                // Apply branch filter
                if (branchId.HasValue)
                {
                    query = query.Where(f => f.BranchId == branchId.Value);
                }
                else if (!userBranches.Contains(0)) // Not global access
                {
                    query = query.Where(f => f.BranchId == null || userBranches.Contains(f.BranchId.Value));
                }

                // Apply supplier filter
                if (supplierId.HasValue)
                {
                    query = query.Where(f => f.SupplierId == supplierId.Value);
                }

                var factures = await query
                    .OrderByDescending(f => f.TotalAmount - f.PaidAmount)
                    .ThenBy(f => f.DueDate)
                    .Take(limit)
                    .Select(f => new FactureListDto
                    {
                        Id = f.Id,
                        SupplierInvoiceNumber = f.SupplierInvoiceNumber,
                        InternalReferenceNumber = f.InternalReferenceNumber,
                        SupplierName = f.Supplier!.CompanyName,
                        InvoiceDate = f.InvoiceDate,
                        DueDate = f.DueDate,
                        TotalAmount = f.TotalAmount,
                        PaidAmount = f.PaidAmount,
                        OutstandingAmount = f.TotalAmount - f.PaidAmount,
                        Status = f.Status,
                        IsOverdue = f.DueDate < now && f.Status != FactureStatus.Paid && f.Status != FactureStatus.Cancelled,
                        DaysOverdue = f.DueDate < now && f.Status != FactureStatus.Paid && f.Status != FactureStatus.Cancelled 
                            ? EF.Functions.DateDiffDay(f.DueDate, now) : 0,
                        DaysUntilDue = f.Status == FactureStatus.Paid || f.Status == FactureStatus.Cancelled 
                            ? 0 : (EF.Functions.DateDiffDay(now, f.DueDate) < 0 ? 0 : EF.Functions.DateDiffDay(now, f.DueDate)),
                        PaymentPriority = (f.Status == FactureStatus.Paid || f.Status == FactureStatus.Cancelled) 
                            ? PaymentPriority.Normal 
                            : (f.DueDate < now || EF.Functions.DateDiffDay(now, f.DueDate) <= 1) 
                                ? PaymentPriority.Urgent 
                                : (EF.Functions.DateDiffDay(now, f.DueDate) <= 7) 
                                    ? PaymentPriority.High 
                                    : PaymentPriority.Normal,
                        BranchName = f.Branch != null ? f.Branch.BranchName : "All Branches",
                        CreatedAt = f.CreatedAt
                    })
                    .ToListAsync();

                return factures;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting outstanding factures");
                throw;
            }
        }

        public async Task<List<OutstandingBySupplierDto>> GetTopSuppliersByFacturesAsync(int requestingUserId, int? branchId = null, DateTime? fromDate = null, DateTime? toDate = null, int limit = 10)
        {
            try
            {
                var user = await _context.Users.FindAsync(requestingUserId);
                var userBranches = user?.CanAccessMultipleBranches == true ? 
                    (user.AccessibleBranchIds?.Split(',').Select(int.Parse).ToList() ?? new List<int> { 0 }) : 
                    new List<int> { user?.BranchId ?? 0 };
                var now = _timezoneService.UtcToLocal(DateTime.UtcNow);
                fromDate ??= now.AddMonths(-3); // Default 3 months
                toDate ??= now;

                var query = _context.Factures
                    .Include(f => f.Supplier)
                    .Where(f => f.InvoiceDate >= fromDate && f.InvoiceDate <= toDate);

                // Apply branch filter
                if (branchId.HasValue)
                {
                    query = query.Where(f => f.BranchId == branchId.Value);
                }
                else if (!userBranches.Contains(0)) // Not global access
                {
                    query = query.Where(f => f.BranchId == null || userBranches.Contains(f.BranchId.Value));
                }

                var topSuppliers = await query
                    .GroupBy(f => new { f.SupplierId, f.Supplier!.SupplierCode, f.Supplier.CompanyName, f.Supplier.ContactPerson, f.Supplier.Phone, f.Supplier.Email })
                    .Select(g => new OutstandingBySupplierDto
                    {
                        SupplierId = g.Key.SupplierId,
                        SupplierCode = g.Key.SupplierCode,
                        CompanyName = g.Key.CompanyName,
                        ContactPerson = g.Key.ContactPerson ?? "",
                        Phone = g.Key.Phone ?? "",
                        Email = g.Key.Email ?? "",
                        TotalOutstandingFactures = g.Count(f => f.Status != FactureStatus.Paid && f.Status != FactureStatus.Cancelled),
                        TotalOutstandingAmount = g.Where(f => f.Status != FactureStatus.Paid && f.Status != FactureStatus.Cancelled).Sum(f => f.TotalAmount - f.PaidAmount),
                        OldestOutstandingDays = g.Where(f => f.Status != FactureStatus.Paid && f.Status != FactureStatus.Cancelled && f.DueDate < now)
                                                 .OrderBy(f => f.DueDate)
                                                 .Select(f => (decimal)EF.Functions.DateDiffDay(f.DueDate, now))
                                                 .FirstOrDefault(),
                        AveragePaymentDelayDays = 0, // Simplified for now - complex payment calculation
                        OldestFactureDueDate = g.Where(f => f.Status != FactureStatus.Paid && f.Status != FactureStatus.Cancelled)
                                                .OrderBy(f => f.DueDate)
                                                .Select(f => (DateTime?)f.DueDate)
                                                .FirstOrDefault(),
                        OverdueCount = g.Count(f => f.DueDate < now && f.Status != FactureStatus.Paid && f.Status != FactureStatus.Cancelled),
                        OverdueAmount = g.Where(f => f.DueDate < now && f.Status != FactureStatus.Paid && f.Status != FactureStatus.Cancelled).Sum(f => f.TotalAmount - f.PaidAmount),
                        PaymentRisk = g.Any(f => f.DueDate < now && f.Status != FactureStatus.Paid && f.Status != FactureStatus.Cancelled && (f.TotalAmount - f.PaidAmount) > 100000000) ? "Critical" :
                                     g.Any(f => f.DueDate < now && f.Status != FactureStatus.Paid && f.Status != FactureStatus.Cancelled && (f.TotalAmount - f.PaidAmount) > 50000000) ? "High" :
                                     g.Any(f => f.DueDate < now && f.Status != FactureStatus.Paid && f.Status != FactureStatus.Cancelled) ? "Medium" : "Low"
                    })
                    .OrderByDescending(s => s.TotalOutstandingAmount)
                    .Take(limit)
                    .ToListAsync();

                // Get top outstanding factures for each supplier
                foreach (var supplier in topSuppliers)
                {
                    supplier.TopOutstandingFactures = await _context.Factures
                        .Where(f => f.SupplierId == supplier.SupplierId && 
                                   f.Status != FactureStatus.Paid && 
                                   f.Status != FactureStatus.Cancelled &&
                                   (f.TotalAmount - f.PaidAmount) > 0)
                        .OrderByDescending(f => f.TotalAmount - f.PaidAmount)
                        .Take(3)
                        .Select(f => new OutstandingFactureBriefDto
                        {
                            Id = f.Id,
                            SupplierInvoiceNumber = f.SupplierInvoiceNumber,
                            InternalReferenceNumber = f.InternalReferenceNumber,
                            OutstandingAmount = f.TotalAmount - f.PaidAmount,
                            DaysOverdue = f.DueDate < now && f.Status != FactureStatus.Paid && f.Status != FactureStatus.Cancelled 
                                ? EF.Functions.DateDiffDay(f.DueDate, now) : 0,
                            DueDate = f.DueDate,
                            Priority = (f.Status == FactureStatus.Paid || f.Status == FactureStatus.Cancelled) 
                                ? PaymentPriority.Normal 
                                : (f.DueDate < now || EF.Functions.DateDiffDay(now, f.DueDate) <= 1) 
                                    ? PaymentPriority.Urgent 
                                    : (EF.Functions.DateDiffDay(now, f.DueDate) <= 7) 
                                        ? PaymentPriority.High 
                                        : PaymentPriority.Normal
                        })
                        .ToListAsync();
                }

                return topSuppliers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top suppliers by factures");
                throw;
            }
        }

        public async Task<List<SuppliersByBranchDto>> GetSuppliersByBranchAsync(int requestingUserId, int? branchId = null)
        {
            try
            {
                var user = await _context.Users.FindAsync(requestingUserId);
                var userBranches = user?.CanAccessMultipleBranches == true ? 
                    (user.AccessibleBranchIds?.Split(',').Select(int.Parse).ToList() ?? new List<int> { 0 }) : 
                    new List<int> { user?.BranchId ?? 0 };
                var now = _timezoneService.UtcToLocal(DateTime.UtcNow);
                var thisMonth = new DateTime(now.Year, now.Month, 1);

                var query = _context.Branches.AsQueryable();

                // Apply branch filter
                if (branchId.HasValue)
                {
                    query = query.Where(b => b.Id == branchId.Value);
                }
                else if (!userBranches.Contains(0)) // Not global access
                {
                    query = query.Where(b => userBranches.Contains(b.Id));
                }

                var branchesData = await query
                    .Select(b => new SuppliersByBranchDto
                    {
                        BranchId = b.Id,
                        BranchCode = b.BranchCode,
                        BranchName = b.BranchName,
                        City = b.City ?? "",
                        TotalSuppliers = _context.Suppliers.Count(s => s.BranchId == b.Id || s.BranchId == null),
                        ActiveSuppliers = _context.Suppliers.Count(s => s.IsActive && (s.BranchId == b.Id || s.BranchId == null)),
                        TotalOutstanding = _context.Factures
                            .Where(f => f.BranchId == b.Id && f.Status != FactureStatus.Paid && f.Status != FactureStatus.Cancelled)
                            .Sum(f => (decimal?)(f.TotalAmount - f.PaidAmount)) ?? 0,
                        AverageFactureAmount = _context.Factures
                            .Where(f => f.BranchId == b.Id && f.InvoiceDate >= thisMonth)
                            .Average(f => (decimal?)f.TotalAmount) ?? 0,
                        TotalFacturesThisMonth = _context.Factures.Count(f => f.BranchId == b.Id && f.InvoiceDate >= thisMonth),
                        PaymentComplianceRate = _context.Factures
                            .Where(f => f.BranchId == b.Id && f.DueDate >= thisMonth.AddMonths(-1) && f.DueDate < thisMonth)
                            .Count() > 0 ? 
                            (decimal)_context.Factures
                                .Where(f => f.BranchId == b.Id && f.DueDate >= thisMonth.AddMonths(-1) && f.DueDate < thisMonth && f.Status == FactureStatus.Paid)
                                .Count() * 100 / 
                            _context.Factures
                                .Where(f => f.BranchId == b.Id && f.DueDate >= thisMonth.AddMonths(-1) && f.DueDate < thisMonth)
                                .Count() : 100
                    })
                    .ToListAsync();

                // Get top suppliers for each branch
                foreach (var branch in branchesData)
                {
                    branch.TopSuppliers = await _context.Factures
                        .Include(f => f.Supplier)
                        .Where(f => f.BranchId == branch.BranchId && f.InvoiceDate >= thisMonth)
                        .GroupBy(f => new { f.SupplierId, f.Supplier!.SupplierCode, f.Supplier.CompanyName })
                        .Select(g => new TopSupplierByBranchDto
                        {
                            SupplierId = g.Key.SupplierId,
                            SupplierCode = g.Key.SupplierCode,
                            CompanyName = g.Key.CompanyName,
                            MonthlySpending = g.Sum(f => f.TotalAmount),
                            FacturesCount = g.Count(),
                            OutstandingAmount = g.Where(f => f.Status != FactureStatus.Paid && f.Status != FactureStatus.Cancelled).Sum(f => f.TotalAmount - f.PaidAmount)
                        })
                        .OrderByDescending(s => s.MonthlySpending)
                        .Take(5)
                        .ToListAsync();

                    // Get category spending (simplified - using supplier as category for now)
                    branch.SpendingByCategory = await _context.Factures
                        .Include(f => f.Supplier)
                        .Where(f => f.BranchId == branch.BranchId && f.InvoiceDate >= thisMonth)
                        .GroupBy(f => f.Supplier!.CompanyName)
                        .Select(g => new CategorySpendingDto
                        {
                            Category = g.Key,
                            Amount = g.Sum(f => f.TotalAmount),
                            FacturesCount = g.Count(),
                            Percentage = 0 // Will be calculated later
                        })
                        .OrderByDescending(c => c.Amount)
                        .Take(5)
                        .ToListAsync();

                    // Calculate percentages
                    var totalAmount = branch.SpendingByCategory.Sum(c => c.Amount);
                    if (totalAmount > 0)
                    {
                        foreach (var category in branch.SpendingByCategory)
                        {
                            category.Percentage = Math.Round(category.Amount * 100 / totalAmount, 2);
                        }
                    }
                }

                return branchesData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting suppliers by branch");
                throw;
            }
        }

        public async Task<SupplierAlertsDto> GetSupplierAlertsAsync(int requestingUserId, int? branchId = null, string? priorityFilter = null)
        {
            try
            {
                var user = await _context.Users.FindAsync(requestingUserId);
                var userBranches = user?.CanAccessMultipleBranches == true ? 
                    (user.AccessibleBranchIds?.Split(',').Select(int.Parse).ToList() ?? new List<int> { 0 }) : 
                    new List<int> { user?.BranchId ?? 0 };
                var now = _timezoneService.UtcToLocal(DateTime.UtcNow);
                var alerts = new List<FactureSupplierAlertDto>();

                var query = _context.Factures
                    .Include(f => f.Supplier)
                    .Include(f => f.Branch)
                    .AsQueryable();

                // Apply branch filter
                if (branchId.HasValue)
                {
                    query = query.Where(f => f.BranchId == branchId.Value);
                }
                else if (!userBranches.Contains(0)) // Not global access
                {
                    query = query.Where(f => f.BranchId == null || userBranches.Contains(f.BranchId.Value));
                }

                // Critical Alerts - Overdue > 30 days with high amounts
                var criticalOverdue = await query
                    .Where(f => f.DueDate < now && f.Status != FactureStatus.Paid && f.Status != FactureStatus.Cancelled && 
                               EF.Functions.DateDiffDay(f.DueDate, now) > 30 && (f.TotalAmount - f.PaidAmount) > 50000000)
                    .Select(f => new FactureSupplierAlertDto
                    {
                        Id = f.Id,
                        AlertType = "CriticalOverdue",
                        Priority = "Critical",
                        Title = "Critical Overdue Payment",
                        Message = $"Facture {f.SupplierInvoiceNumber} is {EF.Functions.DateDiffDay(f.DueDate, now)} days overdue with outstanding amount",
                        SupplierId = f.SupplierId,
                        SupplierName = f.Supplier!.CompanyName,
                        FactureId = f.Id,
                        FactureReference = f.SupplierInvoiceNumber,
                        Amount = f.TotalAmount - f.PaidAmount,
                        CreatedAt = now,
                        DueDate = f.DueDate,
                        DaysOverdue = EF.Functions.DateDiffDay(f.DueDate, now),
                        ActionRequired = "Immediate payment processing required",
                        IsRead = false,
                        BranchId = f.BranchId,
                        BranchName = f.Branch != null ? f.Branch.BranchName : null
                    })
                    .ToListAsync();

                // Warning Alerts - Overdue or high outstanding amounts
                var warningAlerts = await query
                    .Where(f => (f.DueDate < now && f.Status != FactureStatus.Paid && f.Status != FactureStatus.Cancelled && EF.Functions.DateDiffDay(f.DueDate, now) <= 30) || 
                               ((f.TotalAmount - f.PaidAmount) > 100000000 && f.Status != FactureStatus.Paid))
                    .Select(f => new FactureSupplierAlertDto
                    {
                        Id = f.Id,
                        AlertType = (f.DueDate < now && f.Status != FactureStatus.Paid && f.Status != FactureStatus.Cancelled) ? "PaymentOverdue" : "HighOutstanding",
                        Priority = "Warning",
                        Title = (f.DueDate < now && f.Status != FactureStatus.Paid && f.Status != FactureStatus.Cancelled) ? "Payment Overdue" : "High Outstanding Amount",
                        Message = (f.DueDate < now && f.Status != FactureStatus.Paid && f.Status != FactureStatus.Cancelled) ? 
                            $"Facture {f.SupplierInvoiceNumber} is {EF.Functions.DateDiffDay(f.DueDate, now)} days overdue" :
                            $"High outstanding amount",
                        SupplierId = f.SupplierId,
                        SupplierName = f.Supplier!.CompanyName,
                        FactureId = f.Id,
                        FactureReference = f.SupplierInvoiceNumber,
                        Amount = f.TotalAmount - f.PaidAmount,
                        CreatedAt = now,
                        DueDate = f.DueDate,
                        DaysOverdue = (f.DueDate < now && f.Status != FactureStatus.Paid && f.Status != FactureStatus.Cancelled) ? EF.Functions.DateDiffDay(f.DueDate, now) : 0,
                        ActionRequired = (f.DueDate < now && f.Status != FactureStatus.Paid && f.Status != FactureStatus.Cancelled) ? "Schedule payment immediately" : "Review payment schedule",
                        IsRead = false,
                        BranchId = f.BranchId,
                        BranchName = f.Branch != null ? f.Branch.BranchName : null
                    })
                    .ToListAsync();

                // Info Alerts - Due soon
                var infoAlerts = await query
                    .Where(f => EF.Functions.DateDiffDay(now, f.DueDate) <= 7 && EF.Functions.DateDiffDay(now, f.DueDate) > 0 && 
                               f.Status != FactureStatus.Paid && f.Status != FactureStatus.Cancelled)
                    .Select(f => new FactureSupplierAlertDto
                    {
                        Id = f.Id,
                        AlertType = "DueSoon",
                        Priority = "Info",
                        Title = "Payment Due Soon",
                        Message = $"Facture {f.SupplierInvoiceNumber} is due in {EF.Functions.DateDiffDay(now, f.DueDate)} days",
                        SupplierId = f.SupplierId,
                        SupplierName = f.Supplier!.CompanyName,
                        FactureId = f.Id,
                        FactureReference = f.SupplierInvoiceNumber,
                        Amount = f.TotalAmount - f.PaidAmount,
                        CreatedAt = now,
                        DueDate = f.DueDate,
                        DaysOverdue = 0,
                        ActionRequired = "Review and schedule payment",
                        IsRead = false,
                        BranchId = f.BranchId,
                        BranchName = f.Branch != null ? f.Branch.BranchName : null
                    })
                    .ToListAsync();

                var result = new SupplierAlertsDto
                {
                    CriticalAlerts = criticalOverdue,
                    WarningAlerts = warningAlerts,
                    InfoAlerts = infoAlerts,
                    Summary = new SupplierAlertSummaryDto
                    {
                        TotalCriticalAlerts = criticalOverdue.Count,
                        TotalWarningAlerts = warningAlerts.Count,
                        TotalInfoAlerts = infoAlerts.Count,
                        UnreadAlerts = criticalOverdue.Count + warningAlerts.Count + infoAlerts.Count,
                        TotalAmountAtRisk = criticalOverdue.Sum(a => a.Amount ?? 0) + warningAlerts.Sum(a => a.Amount ?? 0),
                        SuppliersWithAlerts = (criticalOverdue.Select(a => a.SupplierId)
                                              .Concat(warningAlerts.Select(a => a.SupplierId))
                                              .Concat(infoAlerts.Select(a => a.SupplierId)))
                                              .Distinct().Count(),
                        LastUpdated = now,
                        AlertsByCategory = new List<AlertCategoryDto>
                        {
                            new() { Category = "CriticalOverdue", Count = criticalOverdue.Count, Priority = "Critical" },
                            new() { Category = "PaymentOverdue", Count = warningAlerts.Count(a => a.AlertType == "PaymentOverdue"), Priority = "Warning" },
                            new() { Category = "HighOutstanding", Count = warningAlerts.Count(a => a.AlertType == "HighOutstanding"), Priority = "Warning" },
                            new() { Category = "DueSoon", Count = infoAlerts.Count, Priority = "Info" }
                        }
                    }
                };

                // Apply priority filter
                if (!string.IsNullOrEmpty(priorityFilter))
                {
                    switch (priorityFilter.ToLower())
                    {
                        case "critical":
                            result.WarningAlerts.Clear();
                            result.InfoAlerts.Clear();
                            break;
                        case "warning":
                            result.CriticalAlerts.Clear();
                            result.InfoAlerts.Clear();
                            break;
                        case "info":
                            result.CriticalAlerts.Clear();
                            result.WarningAlerts.Clear();
                            break;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting supplier alerts");
                throw;
            }
        }

        // ==================== HELPER METHODS ==================== //

        private async Task<FacturePaymentDto> GetPaymentByIdAsync(int paymentId)
        {
            var payment = await _context.FacturePayments
                .Include(p => p.ProcessedByUser)
                .Include(p => p.ApprovedByUser)
                .Include(p => p.ConfirmedByUser)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            return MapToFacturePaymentDto(payment!);
        }

        private async Task<bool> HasFactureAccessAsync(int factureId, int userId)
        {
            var facture = await _context.Factures.FindAsync(factureId);
            if (facture == null) return false;

            var user = await _context.Users.FindAsync(userId);
            var accessibleBranchIds = GetAccessibleBranchIds(user);
            
            if (accessibleBranchIds == null) return true;
            
            return facture.BranchId == null || accessibleBranchIds.Contains(facture.BranchId.Value);
        }

        private List<int>? GetAccessibleBranchIds(User? user)
        {
            if (user == null || user.Role == "SuperAdmin") return null;
            
            if (user.BranchId.HasValue)
                return new List<int> { user.BranchId.Value };
            
            return new List<int>();
        }

        private async Task<decimal> CalculateAveragePaymentDaysAsync(int supplierId)
        {
            var confirmedPayments = await _context.FacturePayments
                .Include(p => p.Facture)
                .Where(p => p.Facture!.SupplierId == supplierId && 
                           p.Status == PaymentStatus.Confirmed &&
                           p.ConfirmedAt.HasValue)
                .ToListAsync();

            if (!confirmedPayments.Any()) return 0;

            var paymentDays = confirmedPayments.Select(p => 
                (p.ConfirmedAt!.Value.Date - p.Facture!.InvoiceDate.Date).Days);

            return (decimal)paymentDays.Average();
        }

        private FactureDto MapToFactureDto(Facture facture)
        {
            return new FactureDto
            {
                Id = facture.Id,
                SupplierInvoiceNumber = facture.SupplierInvoiceNumber,
                InternalReferenceNumber = facture.InternalReferenceNumber,
                SupplierId = facture.SupplierId,
                SupplierName = facture.Supplier?.CompanyName ?? "",
                SupplierCode = facture.Supplier?.SupplierCode ?? "",
                BranchId = facture.BranchId,
                BranchName = facture.Branch?.BranchName ?? "",
                BranchDisplay = facture.Branch?.BranchName ?? "Semua Cabang",
                InvoiceDate = facture.InvoiceDate,
                DueDate = facture.DueDate,
                SupplierPONumber = facture.SupplierPONumber,
                DeliveryDate = facture.DeliveryDate,
                DeliveryNoteNumber = facture.DeliveryNoteNumber,
                TotalAmount = facture.TotalAmount,
                PaidAmount = facture.PaidAmount,
                OutstandingAmount = facture.OutstandingAmount,
                Tax = facture.Tax,
                Discount = facture.Discount,
                Status = facture.Status,
                StatusDisplay = facture.StatusDisplay,
                VerificationStatus = facture.VerificationStatus,
                PaymentPriority = facture.PaymentPriority,
                PriorityDisplay = facture.PriorityDisplay,
                ReceivedBy = facture.ReceivedBy,
                ReceivedByName = facture.ReceivedByUser?.UserProfile?.FullName ?? "",
                ReceivedAt = facture.ReceivedAt,
                VerifiedBy = facture.VerifiedBy,
                VerifiedByName = facture.VerifiedByUser?.UserProfile?.FullName ?? "",
                VerifiedAt = facture.VerifiedAt,
                ApprovedBy = facture.ApprovedBy,
                ApprovedByName = facture.ApprovedByUser?.UserProfile?.FullName ?? "",
                ApprovedAt = facture.ApprovedAt,
                SupplierInvoiceFile = facture.SupplierInvoiceFile,
                ReceiptFile = facture.ReceiptFile,
                SupportingDocs = facture.SupportingDocs,
                Notes = facture.Notes,
                Description = facture.Description,
                DisputeReason = facture.DisputeReason,
                CreatedBy = facture.CreatedBy,
                CreatedByName = facture.CreatedByUser?.UserProfile?.FullName ?? "",
                UpdatedBy = facture.UpdatedBy,
                UpdatedByName = facture.UpdatedByUser?.UserProfile?.FullName ?? "",
                CreatedAt = facture.CreatedAt,
                UpdatedAt = facture.UpdatedAt,
                DaysOverdue = facture.DaysOverdue,
                DaysUntilDue = facture.DaysUntilDue,
                PaymentProgress = facture.PaymentProgress,
                IsOverdue = facture.IsOverdue,
                RequiresApproval = facture.RequiresApproval,
                TotalAmountDisplay = facture.TotalAmountDisplay,
                PaidAmountDisplay = facture.PaidAmountDisplay,
                OutstandingAmountDisplay = facture.OutstandingAmountDisplay,
                CanVerify = facture.CanVerify,
                CanApprove = facture.CanApprove,
                CanDispute = facture.CanDispute,
                CanCancel = facture.CanCancel,
                CanSchedulePayment = facture.CanSchedulePayment,
                CanReceivePayment = facture.CanReceivePayment,
                Items = facture.Items?.Select(MapToFactureItemDto).ToList() ?? new List<FactureItemDto>(),
                Payments = facture.Payments?.Select(MapToFacturePaymentDto).ToList() ?? new List<FacturePaymentDto>()
            };
        }

        private FactureItemDto MapToFactureItemDto(FactureItem item)
        {
            return new FactureItemDto
            {
                Id = item.Id,
                FactureId = item.FactureId,
                ProductId = item.ProductId,
                ProductName = item.Product?.Name,
                ProductBarcode = item.Product?.Barcode,
                SupplierItemCode = item.SupplierItemCode,
                SupplierItemDescription = item.SupplierItemDescription,
                ItemDescription = item.ItemDescription,
                ItemCode = item.ItemCode,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                ReceivedQuantity = item.ReceivedQuantity,
                AcceptedQuantity = item.AcceptedQuantity,
                TaxRate = item.TaxRate,
                DiscountAmount = item.DiscountAmount,
                LineTotal = item.LineTotal,
                TaxAmount = item.TaxAmount,
                LineTotalWithTax = item.LineTotalWithTax,
                Notes = item.Notes,
                VerificationNotes = item.VerificationNotes,
                IsVerified = item.IsVerified,
                VerifiedAt = item.VerifiedAt,
                VerifiedByName = item.VerifiedByUser?.UserProfile?.FullName,
                IsProductMapped = item.IsProductMapped,
                HasQuantityVariance = item.HasQuantityVariance,
                HasAcceptanceVariance = item.HasAcceptanceVariance,
                VerificationStatus = item.VerificationStatus,
                QuantityVariance = item.QuantityVariance,
                AcceptanceVariance = item.AcceptanceVariance,
                UnitDisplay = item.UnitDisplay,
                UnitPriceDisplay = item.UnitPriceDisplay,
                LineTotalDisplay = item.LineTotalDisplay,
                LineTotalWithTaxDisplay = item.LineTotalWithTaxDisplay,
                RequiresApproval = item.RequiresApproval,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt
            };
        }

        private FacturePaymentDto MapToFacturePaymentDto(FacturePayment payment)
        {
            return new FacturePaymentDto
            {
                Id = payment.Id,
                FactureId = payment.FactureId,
                PaymentDate = payment.PaymentDate,
                Amount = payment.Amount,
                PaymentMethod = payment.PaymentMethod,
                PaymentMethodDisplay = payment.PaymentMethodDisplay,
                Status = payment.Status,
                StatusDisplay = payment.StatusDisplay,
                OurPaymentReference = payment.OurPaymentReference,
                SupplierAckReference = payment.SupplierAckReference,
                BankAccount = payment.BankAccount,
                CheckNumber = payment.CheckNumber,
                TransferReference = payment.TransferReference,
                PaymentReference = payment.PaymentReference,
                ProcessedBy = payment.ProcessedBy,
                ProcessedByName = payment.ProcessedByUser?.UserProfile?.FullName ?? "",
                ApprovedBy = payment.ApprovedBy,
                ApprovedByName = payment.ApprovedByUser?.UserProfile?.FullName,
                ApprovedAt = payment.ApprovedAt,
                ConfirmedAt = payment.ConfirmedAt,
                ConfirmedByName = payment.ConfirmedByUser?.UserProfile?.FullName,
                Notes = payment.Notes,
                FailureReason = payment.FailureReason,
                DisputeReason = payment.DisputeReason,
                PaymentReceiptFile = payment.PaymentReceiptFile,
                ConfirmationFile = payment.ConfirmationFile,
                ScheduledDate = payment.ScheduledDate,
                RequiresApproval = payment.RequiresApproval,
                IsOverdue = payment.IsOverdue,
                DaysOverdue = payment.DaysOverdue,
                DaysUntilPayment = payment.DaysUntilPayment,
                IsDueToday = payment.IsDueToday,
                IsDueSoon = payment.IsDueSoon,
                ProcessingStatus = payment.ProcessingStatus,
                HasConfirmation = payment.HasConfirmation,
                AmountDisplay = payment.AmountDisplay,
                CanEdit = payment.CanEdit,
                CanProcess = payment.CanProcess,
                CanConfirm = payment.CanConfirm,
                CanCancel = payment.CanCancel,
                CreatedAt = payment.CreatedAt,
                UpdatedAt = payment.UpdatedAt
            };
        }
    }
}