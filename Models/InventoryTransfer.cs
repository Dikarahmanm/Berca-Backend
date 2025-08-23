using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Berca_Backend.Models
{
    /// <summary>
    /// Inventory Transfer between branches for Toko Eniwan multi-branch management
    /// </summary>
    public class InventoryTransfer
    {
        public int Id { get; set; }

        // Transfer Identification
        [Required]
        [StringLength(50)]
        public string TransferNumber { get; set; } = string.Empty; // AUTO: TF-YYYYMMDD-XXXX

        [Required]
        public TransferStatus Status { get; set; } = TransferStatus.Pending;

        [Required]
        public TransferType Type { get; set; } = TransferType.Regular;

        [Required]
        public TransferPriority Priority { get; set; } = TransferPriority.Normal;

        // Source and Destination
        [Required]
        public int SourceBranchId { get; set; }
        public virtual Branch SourceBranch { get; set; } = null!;

        [Required]
        public int DestinationBranchId { get; set; }
        public virtual Branch DestinationBranch { get; set; } = null!;

        // Transfer Details
        [Required]
        [StringLength(500)]
        public string RequestReason { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Notes { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal EstimatedCost { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal ActualCost { get; set; } = 0;

        // Transfer Items
        public virtual ICollection<InventoryTransferItem> TransferItems { get; set; } = new List<InventoryTransferItem>();

        // Workflow & Approval
        public int RequestedBy { get; set; }
        public virtual User RequestedByUser { get; set; } = null!;

        public int? ApprovedBy { get; set; }
        public virtual User? ApprovedByUser { get; set; }

        public DateTime? ApprovedAt { get; set; }

        public int? ShippedBy { get; set; }
        public virtual User? ShippedByUser { get; set; }

        public DateTime? ShippedAt { get; set; }

        public int? ReceivedBy { get; set; }
        public virtual User? ReceivedByUser { get; set; }

        public DateTime? ReceivedAt { get; set; }

        public int? CancelledBy { get; set; }
        public virtual User? CancelledByUser { get; set; }

        public DateTime? CancelledAt { get; set; }

        [StringLength(500)]
        public string? CancellationReason { get; set; }

        // Logistics Information
        [StringLength(100)]
        public string? LogisticsProvider { get; set; }

        [StringLength(100)]
        public string? TrackingNumber { get; set; }

        public DateTime? EstimatedDeliveryDate { get; set; }

        public decimal DistanceKm { get; set; } = 0;

        // Audit Trail
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Computed Properties
        [NotMapped]
        public int TotalItems => TransferItems?.Sum(ti => ti.Quantity) ?? 0;

        [NotMapped]
        public decimal TotalValue => TransferItems?.Sum(ti => ti.Quantity * ti.UnitCost) ?? 0;

        [NotMapped]
        public bool RequiresManagerApproval => TotalValue > 5000000; // 5M IDR

        [NotMapped]
        public bool IsEmergencyTransfer => Priority == TransferPriority.Emergency;

        // Compatibility properties for service layer
        [NotMapped]
        public int FromBranchId { get => SourceBranchId; set => SourceBranchId = value; }
        
        [NotMapped]
        public int ToBranchId { get => DestinationBranchId; set => DestinationBranchId = value; }
        
        [NotMapped]
        public DateTime TransferDate { get => CreatedAt; set => CreatedAt = value; }
        
        [NotMapped]
        public string Reason { get => RequestReason; set => RequestReason = value; }
        
        // For single-product transfers - simplified access
        [NotMapped]
        public int? ProductId 
        { 
            get => TransferItems?.FirstOrDefault()?.ProductId; 
            set 
            {
                // For single-product transfers, create or update first item
                if (value.HasValue && TransferItems != null && TransferItems.Any())
                {
                    var firstItem = TransferItems.First();
                    firstItem.ProductId = value.Value;
                }
            }
        }
        
        [NotMapped]
        public int Quantity 
        { 
            get => TransferItems?.Sum(ti => ti.Quantity) ?? 0;
            set
            {
                // For single-product transfers, create or update first item quantity
                if (TransferItems != null && TransferItems.Any())
                {
                    var firstItem = TransferItems.First();
                    firstItem.Quantity = value;
                }
            }
        }

        [NotMapped]
        public bool CanBeApproved => Status == TransferStatus.Pending;

        [NotMapped]
        public bool CanBeShipped => Status == TransferStatus.Approved;

        [NotMapped]
        public bool CanBeReceived => Status == TransferStatus.InTransit;

        [NotMapped]
        public bool CanBeCancelled => Status is TransferStatus.Pending or TransferStatus.Approved;

        [NotMapped]
        public string StatusDisplay => Status switch
        {
            TransferStatus.Pending => "Menunggu Persetujuan",
            TransferStatus.Approved => "Disetujui",
            TransferStatus.InTransit => "Dalam Perjalanan",
            TransferStatus.Completed => "Selesai",
            TransferStatus.Cancelled => "Dibatalkan",
            TransferStatus.Rejected => "Ditolak",
            _ => "Unknown"
        };

        [NotMapped]
        public string TypeDisplay => Type switch
        {
            TransferType.Regular => "Transfer Reguler",
            TransferType.Emergency => "Transfer Darurat",
            TransferType.Rebalancing => "Rebalancing Stok",
            TransferType.Bulk => "Transfer Bulk",
            _ => "Unknown"
        };

        [NotMapped]
        public string PriorityDisplay => Priority switch
        {
            TransferPriority.Low => "Rendah",
            TransferPriority.Normal => "Normal",
            TransferPriority.High => "Tinggi",
            TransferPriority.Emergency => "Darurat",
            _ => "Unknown"
        };

        [NotMapped]
        public TimeSpan? ProcessingTime => CompletedAt - CreatedAt;

        [NotMapped]
        public DateTime? CompletedAt => Status switch
        {
            TransferStatus.Completed => ReceivedAt,
            TransferStatus.Cancelled => CancelledAt,
            TransferStatus.Rejected => CancelledAt,
            _ => null
        };
    }

    /// <summary>
    /// Individual items in an inventory transfer
    /// </summary>
    public class InventoryTransferItem
    {
        public int Id { get; set; }

        // Transfer Reference
        [Required]
        public int InventoryTransferId { get; set; }
        public virtual InventoryTransfer InventoryTransfer { get; set; } = null!;

        // Product Information
        [Required]
        public int ProductId { get; set; }
        public virtual Product Product { get; set; } = null!;

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public int Quantity { get; set; }

        // Cost Information
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitCost { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalCost { get; set; } = 0;

        // Inventory Tracking
        public int SourceStockBefore { get; set; }
        public int SourceStockAfter { get; set; }
        public int? DestinationStockBefore { get; set; }
        public int? DestinationStockAfter { get; set; }

        // Quality & Expiry
        public DateTime? ExpiryDate { get; set; }
        public string? BatchNumber { get; set; }

        [StringLength(200)]
        public string? QualityNotes { get; set; }

        // Computed Properties
        [NotMapped]
        public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.UtcNow;

        [NotMapped]
        public bool IsNearExpiry => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.UtcNow.AddDays(30);
    }

    /// <summary>
    /// Transfer status history for audit trail
    /// </summary>
    public class InventoryTransferStatusHistory
    {
        public int Id { get; set; }

        [Required]
        public int InventoryTransferId { get; set; }
        public virtual InventoryTransfer InventoryTransfer { get; set; } = null!;

        [Required]
        public TransferStatus FromStatus { get; set; }

        [Required]
        public TransferStatus ToStatus { get; set; }

        public int ChangedBy { get; set; }
        public virtual User ChangedByUser { get; set; } = null!;

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        [StringLength(500)]
        public string? Reason { get; set; }
    }

    // ==================== ENUMS ==================== //

    public enum TransferStatus
    {
        Pending = 0,     // Waiting for approval
        Approved = 1,    // Approved and ready to ship
        InTransit = 2,   // Being shipped
        Completed = 3,   // Received and completed
        Cancelled = 4,   // Cancelled by user
        Rejected = 5     // Rejected during approval
    }

    public enum TransferType
    {
        Regular = 0,     // Standard transfer
        Emergency = 1,   // Emergency/urgent transfer
        Rebalancing = 2, // Stock rebalancing
        Bulk = 3         // Bulk transfer
    }

    public enum TransferPriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Emergency = 3
    }

    // Alias for service compatibility
    public enum InventoryTransferStatus
    {
        Pending = 0,     // Waiting for approval
        Approved = 1,    // Approved and ready to ship
        InTransit = 2,   // Being shipped
        Completed = 3,   // Received and completed
        Cancelled = 4,   // Cancelled by user
        Rejected = 5     // Rejected during approval
    }
}