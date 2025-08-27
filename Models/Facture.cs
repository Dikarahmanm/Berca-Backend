using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;

namespace Berca_Backend.Models
{
    /// <summary>
    /// Facture Status enumeration for supplier invoice workflow
    /// </summary>
    public enum FactureStatus
    {
        Received = 0,      // Invoice received from supplier
        Verified = 1,      // Items verified against delivery
        Approved = 2,      // Approved for payment
        PartiallyPaid = 3, // Partial payment made
        Paid = 4,          // Fully paid
        Overdue = 5,       // Payment overdue
        Disputed = 6,      // Under dispute with supplier
        Cancelled = 7      // Cancelled/rejected
    }

    /// <summary>
    /// Payment Priority enumeration based on due date proximity
    /// </summary>
    public enum PaymentPriority
    {
        Normal = 0,
        High = 1,
        Urgent = 2
    }

    /// <summary>
    /// Payment Method enumeration for supplier payments
    /// </summary>
    public enum PaymentMethod
    {
        BankTransfer = 0,
        Check = 1,
        Cash = 2,
        CreditCard = 3,
        DigitalPayment = 4
    }

    /// <summary>
    /// Payment Status enumeration for our payment workflow to suppliers
    /// </summary>
    public enum PaymentStatus
    {
        Scheduled = 0,  // Payment scheduled
        Processed = 1,  // Payment executed by us
        Confirmed = 2,  // Confirmed received by supplier
        Failed = 3,     // Payment failed
        Disputed = 4    // Payment under dispute
    }

    /// <summary>
    /// Facture (Supplier Invoice) entity for managing supplier invoices we receive
    /// Focus on receiving, verifying, and paying supplier invoices
    /// </summary>
    public class Facture
    {
        public int Id { get; set; }

        // Supplier invoice identification
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string SupplierInvoiceNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string InternalReferenceNumber { get; set; } = string.Empty;

        // Supplier relationship
        [Required]
        public int SupplierId { get; set; }
        public virtual Supplier? Supplier { get; set; }

        // Branch relationship for multi-tenant support
        public int? BranchId { get; set; }
        public virtual Branch? Branch { get; set; }

        // Invoice details from supplier
        [Required]
        public DateTime InvoiceDate { get; set; }

        [Required]
        public DateTime DueDate { get; set; }

        [StringLength(50)]
        public string? SupplierPONumber { get; set; }

        public DateTime? DeliveryDate { get; set; }

        [StringLength(50)]
        public string? DeliveryNoteNumber { get; set; }

        // Financial amounts
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999.99)]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999.99)]
        public decimal PaidAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 100)]
        public decimal Tax { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999.99)]
        public decimal Discount { get; set; } = 0;

        // Status and workflow
        [Required]
        public FactureStatus Status { get; set; } = FactureStatus.Received;

        // Workflow tracking
        public int? ReceivedBy { get; set; }
        public virtual User? ReceivedByUser { get; set; }
        public DateTime? ReceivedAt { get; set; }

        public int? VerifiedBy { get; set; }
        public virtual User? VerifiedByUser { get; set; }
        public DateTime? VerifiedAt { get; set; }

        public int? ApprovedBy { get; set; }
        public virtual User? ApprovedByUser { get; set; }
        public DateTime? ApprovedAt { get; set; }

        // File attachments
        [StringLength(500)]
        public string? SupplierInvoiceFile { get; set; }

        [StringLength(500)]
        public string? ReceiptFile { get; set; }

        [StringLength(500)]
        public string? SupportingDocs { get; set; }

        // Notes and description
        [StringLength(2000)]
        public string? Notes { get; set; }

        [StringLength(2000)]
        public string? Description { get; set; }

        [StringLength(1000)]
        public string? DisputeReason { get; set; }

        // Audit fields
        [Required]
        public int CreatedBy { get; set; }
        public virtual User? CreatedByUser { get; set; }

        public int? UpdatedBy { get; set; }
        public virtual User? UpdatedByUser { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<FactureItem> Items { get; set; } = new List<FactureItem>();
        public virtual ICollection<FacturePayment> Payments { get; set; } = new List<FacturePayment>();

        // Computed properties
        [NotMapped]
        public decimal OutstandingAmount => TotalAmount - PaidAmount;

        [NotMapped]
        public bool IsOverdue => DueDate < DateTime.UtcNow && Status != FactureStatus.Paid && Status != FactureStatus.Cancelled;

        [NotMapped]
        public int DaysOverdue
        {
            get
            {
                if (!IsOverdue) return 0;
                return (DateTime.UtcNow.Date - DueDate.Date).Days;
            }
        }

        [NotMapped]
        public int DaysUntilDue
        {
            get
            {
                if (Status == FactureStatus.Paid || Status == FactureStatus.Cancelled) return 0;
                var days = (DueDate.Date - DateTime.UtcNow.Date).Days;
                return Math.Max(0, days);
            }
        }

        [NotMapped]
        public decimal PaymentProgress
        {
            get
            {
                if (TotalAmount == 0) return 0;
                return Math.Round((PaidAmount / TotalAmount) * 100, 2);
            }
        }

        [NotMapped]
        public PaymentPriority PaymentPriority
        {
            get
            {
                if (Status == FactureStatus.Paid || Status == FactureStatus.Cancelled)
                    return PaymentPriority.Normal;

                if (IsOverdue || DaysUntilDue <= 1)
                    return PaymentPriority.Urgent;

                if (DaysUntilDue <= 7)
                    return PaymentPriority.High;

                return PaymentPriority.Normal;
            }
        }

        [NotMapped]
        public string StatusDisplay
        {
            get
            {
                return Status switch
                {
                    FactureStatus.Received => "Diterima",
                    FactureStatus.Verified => "Diverifikasi",
                    FactureStatus.Approved => "Disetujui",
                    FactureStatus.PartiallyPaid => "Dibayar Sebagian",
                    FactureStatus.Paid => "Lunas",
                    FactureStatus.Overdue => "Terlambat",
                    FactureStatus.Disputed => "Disengketakan",
                    FactureStatus.Cancelled => "Dibatalkan",
                    _ => "Unknown"
                };
            }
        }

        [NotMapped]
        public string PriorityDisplay
        {
            get
            {
                return PaymentPriority switch
                {
                    PaymentPriority.Normal => "Normal",
                    PaymentPriority.High => "Tinggi",
                    PaymentPriority.Urgent => "Mendesak",
                    _ => "Normal"
                };
            }
        }

        [NotMapped]
        public string VerificationStatus
        {
            get
            {
                return Status switch
                {
                    FactureStatus.Received => "Menunggu Verifikasi",
                    FactureStatus.Verified => "Sudah Diverifikasi",
                    FactureStatus.Approved => "Disetujui",
                    _ => "Selesai"
                };
            }
        }

        [NotMapped]
        public string TotalAmountDisplay => TotalAmount.ToString("C", new CultureInfo("id-ID"));

        [NotMapped]
        public string PaidAmountDisplay => PaidAmount.ToString("C", new CultureInfo("id-ID"));

        [NotMapped]
        public string OutstandingAmountDisplay => OutstandingAmount.ToString("C", new CultureInfo("id-ID"));

        [NotMapped]
        public string BranchDisplay => Branch?.BranchName ?? "Semua Cabang";

        [NotMapped]
        public bool RequiresApproval => TotalAmount >= 50000000; // 50M IDR threshold

        [NotMapped]
        public bool CanVerify => Status == FactureStatus.Received;

        [NotMapped]
        public bool CanApprove => Status == FactureStatus.Verified && ApprovedBy == null;

        [NotMapped]
        public bool CanDispute => Status != FactureStatus.Paid && Status != FactureStatus.Cancelled && Status != FactureStatus.Disputed;

        [NotMapped]
        public bool CanCancel => Status != FactureStatus.Paid && Status != FactureStatus.Cancelled;

        [NotMapped]
        public bool CanSchedulePayment => (Status == FactureStatus.Approved || Status == FactureStatus.PartiallyPaid) && OutstandingAmount > 0;

        [NotMapped]
        public bool CanReceivePayment => Status != FactureStatus.Paid && Status != FactureStatus.Cancelled && OutstandingAmount > 0;
    }

}