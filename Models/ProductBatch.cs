using System.ComponentModel.DataAnnotations;

namespace Berca_Backend.Models
{
    /// <summary>
    /// Product Batch model for tracking expiry dates and batch numbers
    /// Enables FIFO (First In First Out) management for products with expiry dates
    /// </summary>
    public class ProductBatch
    {
        public int Id { get; set; }

        /// <summary>
        /// Reference to the Product this batch belongs to
        /// </summary>
        [Required]
        public int ProductId { get; set; }

        /// <summary>
        /// Batch number for tracking (e.g., "BATCH-20240815-001")
        /// </summary>
        [Required]
        [StringLength(50)]
        public string BatchNumber { get; set; } = string.Empty;

        /// <summary>
        /// Expiry date for this batch (required for categories that require expiry tracking)
        /// </summary>
        public DateTime? ExpiryDate { get; set; }

        /// <summary>
        /// Manufacturing/Production date
        /// </summary>
        public DateTime? ProductionDate { get; set; }

        /// <summary>
        /// Current stock quantity for this specific batch
        /// </summary>
        [Range(0, int.MaxValue)]
        public int CurrentStock { get; set; } = 0;

        /// <summary>
        /// Initial stock quantity when batch was created
        /// </summary>
        [Range(0, int.MaxValue)]
        public int InitialStock { get; set; } = 0;

        /// <summary>
        /// Cost per unit for this batch (for accurate FIFO costing)
        /// </summary>
        [Range(0, double.MaxValue)]
        public decimal CostPerUnit { get; set; } = 0;

        /// <summary>
        /// Supplier information for this batch
        /// </summary>
        [StringLength(100)]
        public string? SupplierName { get; set; }

        /// <summary>
        /// Purchase order reference
        /// </summary>
        [StringLength(50)]
        public string? PurchaseOrderNumber { get; set; }

        /// <summary>
        /// Notes about this batch (quality, storage conditions, etc.)
        /// </summary>
        [StringLength(500)]
        public string? Notes { get; set; }

        /// <summary>
        /// Flag to indicate if this batch is blocked/quarantined
        /// </summary>
        public bool IsBlocked { get; set; } = false;

        /// <summary>
        /// Reason for blocking (quality issues, recall, etc.)
        /// </summary>
        [StringLength(200)]
        public string? BlockReason { get; set; }

        /// <summary>
        /// Flag to indicate if this batch has expired
        /// </summary>
        public bool IsExpired { get; set; } = false;

        /// <summary>
        /// Flag to indicate if expired products have been disposed
        /// </summary>
        public bool IsDisposed { get; set; } = false;

        /// <summary>
        /// Date when expired products were disposed
        /// </summary>
        public DateTime? DisposalDate { get; set; }

        /// <summary>
        /// User who marked products as disposed
        /// </summary>
        public int? DisposedByUserId { get; set; }

        /// <summary>
        /// Method of disposal (return to supplier, waste disposal, donation, etc.)
        /// </summary>
        [StringLength(100)]
        public string? DisposalMethod { get; set; }

        /// <summary>
        /// Branch where this batch is located
        /// </summary>
        public int? BranchId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public int CreatedByUserId { get; set; }
        public int UpdatedByUserId { get; set; }

        // Navigation Properties
        public virtual Product? Product { get; set; }
        public virtual Branch? Branch { get; set; }
        public virtual User? CreatedByUser { get; set; }
        public virtual User? UpdatedByUser { get; set; }
        public virtual User? DisposedByUser { get; set; }

        // Computed Properties
        /// <summary>
        /// Days until expiry (negative if expired)
        /// </summary>
        public int? DaysUntilExpiry
        {
            get
            {
                if (!ExpiryDate.HasValue) return null;
                return (int)(ExpiryDate.Value.Date - DateTime.UtcNow.Date).TotalDays;
            }
        }

        /// <summary>
        /// Expiry status based on current date
        /// </summary>
        public ExpiryStatus ExpiryStatus
        {
            get
            {
                if (!ExpiryDate.HasValue) return ExpiryStatus.NoExpiry;
                
                var daysUntilExpiry = DaysUntilExpiry;
                if (!daysUntilExpiry.HasValue) return ExpiryStatus.NoExpiry;

                if (daysUntilExpiry < 0) return ExpiryStatus.Expired;
                if (daysUntilExpiry <= 3) return ExpiryStatus.Critical;
                if (daysUntilExpiry <= 7) return ExpiryStatus.Warning;
                if (daysUntilExpiry <= 30) return ExpiryStatus.Normal;
                return ExpiryStatus.Good;
            }
        }

        /// <summary>
        /// Available stock for sale (excludes blocked/expired stock)
        /// </summary>
        public int AvailableStock
        {
            get
            {
                if (IsBlocked || IsExpired || IsDisposed) return 0;
                return CurrentStock;
            }
        }
    }

    /// <summary>
    /// Expiry status enumeration for batch tracking
    /// </summary>
    public enum ExpiryStatus
    {
        NoExpiry = 0,    // Product doesn't have expiry date
        Good = 1,        // More than 30 days until expiry
        Normal = 2,      // 8-30 days until expiry
        Warning = 3,     // 4-7 days until expiry
        Critical = 4,    // 1-3 days until expiry
        Expired = 5      // Already expired
    }

    /// <summary>
    /// Disposal method enumeration
    /// </summary>
    public enum DisposalMethod
    {
        ReturnToSupplier = 0,
        WasteDisposal = 1,
        Donation = 2,
        Internal = 3,
        Recall = 4
    }
}