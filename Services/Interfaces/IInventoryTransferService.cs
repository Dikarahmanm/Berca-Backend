using Berca_Backend.DTOs;
using Berca_Backend.Models;

namespace Berca_Backend.Services.Interfaces
{
    /// <summary>
    /// Interface for Inventory Transfer Service
    /// Handles multi-branch inventory transfers for Toko Eniwan
    /// </summary>
    public interface IInventoryTransferService
    {
        // ==================== CORE TRANSFER OPERATIONS ==================== //
        
        /// <summary>
        /// Create a new inventory transfer request
        /// </summary>
        Task<InventoryTransferDto> CreateTransferRequestAsync(CreateInventoryTransferRequestDto request, int requestingUserId);
        
        /// <summary>
        /// Create bulk transfer request for multiple products
        /// </summary>
        Task<InventoryTransferDto> CreateBulkTransferRequestAsync(BulkTransferRequestDto request, int requestingUserId);
        
        /// <summary>
        /// Get transfer by ID with full details
        /// </summary>
        Task<InventoryTransferDto?> GetTransferByIdAsync(int transferId, int requestingUserId);
        
        /// <summary>
        /// Get transfers with filtering and pagination
        /// </summary>
        Task<(List<InventoryTransferSummaryDto> Transfers, int TotalCount)> GetTransfersAsync(InventoryTransferQueryParams queryParams, int requestingUserId);
        
        /// <summary>
        /// Get transfers accessible to user (based on branch permissions)
        /// </summary>
        Task<List<int>> GetAccessibleTransferIdsForUserAsync(int userId);

        // ==================== TRANSFER WORKFLOW ==================== //
        
        /// <summary>
        /// Approve or reject a transfer request
        /// </summary>
        Task<InventoryTransferDto> ApproveTransferAsync(int transferId, TransferApprovalRequestDto approval, int approvingUserId);
        
        /// <summary>
        /// Mark transfer as shipped/in transit
        /// </summary>
        Task<InventoryTransferDto> ShipTransferAsync(int transferId, TransferShipmentRequestDto shipment, int shippingUserId);
        
        /// <summary>
        /// Complete transfer receipt at destination
        /// </summary>
        Task<InventoryTransferDto> ReceiveTransferAsync(int transferId, TransferReceiptRequestDto receipt, int receivingUserId);
        
        /// <summary>
        /// Cancel a transfer (if allowed by status)
        /// </summary>
        Task<InventoryTransferDto> CancelTransferAsync(int transferId, string cancellationReason, int cancellingUserId);

        // ==================== BUSINESS LOGIC & VALIDATION ==================== //
        
        /// <summary>
        /// Validate if transfer can be created (stock availability, business rules)
        /// </summary>
        Task<(bool IsValid, List<string> ValidationErrors)> ValidateTransferRequestAsync(CreateInventoryTransferRequestDto request, int requestingUserId);
        
        /// <summary>
        /// Calculate transfer cost based on distance and logistics
        /// </summary>
        Task<decimal> CalculateTransferCostAsync(int sourceBranchId, int destinationBranchId, List<CreateTransferItemDto> items);
        
        /// <summary>
        /// Calculate distance between branches
        /// </summary>
        Task<decimal> CalculateDistanceBetweenBranchesAsync(int sourceBranchId, int destinationBranchId);
        
        /// <summary>
        /// Check if user can approve transfer (authorization rules)
        /// </summary>
        Task<bool> CanUserApproveTransferAsync(int transferId, int userId);
        
        /// <summary>
        /// Check if transfer requires manager approval (high value, etc.)
        /// </summary>
        Task<bool> RequiresManagerApprovalAsync(int transferId);

        // ==================== STOCK MANAGEMENT ==================== //
        
        /// <summary>
        /// Reserve stock for approved transfer (prevent overselling)
        /// </summary>
        Task ReserveStockForTransferAsync(int transferId);
        
        /// <summary>
        /// Release reserved stock (on cancellation)
        /// </summary>
        Task ReleaseReservedStockAsync(int transferId);
        
        /// <summary>
        /// Update inventory levels on transfer completion
        /// </summary>
        Task UpdateInventoryOnTransferCompletionAsync(int transferId);
        
        /// <summary>
        /// Create inventory mutations for audit trail
        /// </summary>
        Task CreateInventoryMutationsForTransferAsync(int transferId, MutationType mutationType);

        // ==================== EMERGENCY & ALERTS ==================== //
        
        /// <summary>
        /// Create emergency transfer for critical stock shortage
        /// </summary>
        Task<InventoryTransferDto> CreateEmergencyTransferAsync(int productId, int destinationBranchId, int quantity, int requestingUserId);
        
        /// <summary>
        /// Get emergency transfer suggestions based on low stock alerts
        /// </summary>
        Task<List<EmergencyTransferSuggestionDto>> GetEmergencyTransferSuggestionsAsync(int? branchId = null);
        
        /// <summary>
        /// Check for products requiring emergency transfers
        /// </summary>
        Task<List<int>> GetProductsRequiringEmergencyTransferAsync(int branchId);

        // ==================== ANALYTICS & REPORTING ==================== //
        
        /// <summary>
        /// Get comprehensive transfer analytics
        /// </summary>
        Task<TransferAnalyticsDto> GetTransferAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null, int? requestingUserId = null);
        
        /// <summary>
        /// Get transfer suggestions for stock optimization
        /// </summary>
        Task<TransferSuggestionsDto> GetTransferSuggestionsAsync(int? requestingUserId = null);
        
        /// <summary>
        /// Get branch transfer performance metrics
        /// </summary>
        Task<List<BranchTransferStatsDto>> GetBranchTransferStatsAsync(DateTime? startDate = null, DateTime? endDate = null, int? requestingUserId = null);
        
        /// <summary>
        /// Get transfer trends analysis
        /// </summary>
        Task<List<TransferTrendDto>> GetTransferTrendsAsync(DateTime startDate, DateTime endDate, int? requestingUserId = null);

        // ==================== OPTIMIZATION & RECOMMENDATIONS ==================== //
        
        /// <summary>
        /// Get stock rebalancing suggestions between branches
        /// </summary>
        Task<List<StockRebalancingSuggestionDto>> GetStockRebalancingSuggestionsAsync(int? requestingUserId = null);
        
        /// <summary>
        /// Get route optimization suggestions for better logistics
        /// </summary>
        Task<List<TransferEfficiencyDto>> GetRouteOptimizationSuggestionsAsync(int? requestingUserId = null);
        
        /// <summary>
        /// Generate automatic transfer recommendations based on stock levels
        /// </summary>
        Task<List<InventoryTransferDto>> GenerateAutomaticTransferRecommendationsAsync();

        // ==================== AUDIT & HISTORY ==================== //
        
        /// <summary>
        /// Get transfer status history for audit trail
        /// </summary>
        Task<List<InventoryTransferStatusHistory>> GetTransferStatusHistoryAsync(int transferId);
        
        /// <summary>
        /// Log transfer status change
        /// </summary>
        Task LogTransferStatusChangeAsync(int transferId, TransferStatus fromStatus, TransferStatus toStatus, int changedBy, string? reason = null);
        
        /// <summary>
        /// Get transfer activity summary for dashboard
        /// </summary>
        Task<object> GetTransferActivitySummaryAsync(int? branchId = null, int? requestingUserId = null);

        // ==================== UTILITY METHODS ==================== //
        
        /// <summary>
        /// Generate unique transfer number
        /// </summary>
        Task<string> GenerateTransferNumberAsync();
        
        /// <summary>
        /// Check if user has access to transfer (branch permissions)
        /// </summary>
        Task<bool> CanUserAccessTransferAsync(int transferId, int userId);
        
        /// <summary>
        /// Get available source branches for product transfer
        /// </summary>
        Task<List<AvailableSourceDto>> GetAvailableSourceBranchesAsync(int productId, int destinationBranchId, int requiredQuantity);
        
        /// <summary>
        /// Estimate delivery date based on distance and logistics
        /// </summary>
        Task<DateTime> EstimateDeliveryDateAsync(int sourceBranchId, int destinationBranchId, TransferPriority priority = TransferPriority.Normal);
    }
}