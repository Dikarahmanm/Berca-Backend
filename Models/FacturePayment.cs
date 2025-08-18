using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;

namespace Berca_Backend.Models
{
    /// <summary>
    /// FacturePayment entity for tracking our payments to suppliers
    /// Includes payment workflow with scheduling, processing, and confirmation
    /// </summary>
    public class FacturePayment
    {
        public int Id { get; set; }

        // Facture relationship
        [Required]
        public int FactureId { get; set; }
        public virtual Facture? Facture { get; set; }

        // Payment details
        [Required]
        public DateTime PaymentDate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999.99)]
        public decimal Amount { get; set; }

        [Required]
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.BankTransfer;

        [Required]
        public PaymentStatus Status { get; set; } = PaymentStatus.Scheduled;

        // Payment references
        [StringLength(100)]
        public string? OurPaymentReference { get; set; }

        [StringLength(100)]
        public string? SupplierAckReference { get; set; }

        [StringLength(100)]
        public string? BankAccount { get; set; }

        [StringLength(50)]
        public string? CheckNumber { get; set; }

        [StringLength(100)]
        public string? TransferReference { get; set; }

        // Workflow tracking
        [Required]
        public int ProcessedBy { get; set; }
        public virtual User? ProcessedByUser { get; set; }

        public int? ApprovedBy { get; set; }
        public virtual User? ApprovedByUser { get; set; }

        public DateTime? ApprovedAt { get; set; }

        public DateTime? ConfirmedAt { get; set; }

        public int? ConfirmedBy { get; set; }
        public virtual User? ConfirmedByUser { get; set; }

        // Payment notes and details
        [StringLength(1000)]
        public string? Notes { get; set; }

        [StringLength(1000)]
        public string? FailureReason { get; set; }

        [StringLength(500)]
        public string? DisputeReason { get; set; }

        // Receipt and proof
        [StringLength(500)]
        public string? PaymentReceiptFile { get; set; }

        [StringLength(500)]
        public string? ConfirmationFile { get; set; }

        // Scheduled payment details
        public DateTime? ScheduledDate { get; set; }

        public bool IsRecurring { get; set; } = false;

        [StringLength(50)]
        public string? RecurrencePattern { get; set; }

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

        // Computed properties
        [NotMapped]
        public string AmountDisplay => Amount.ToString("C", new CultureInfo("id-ID"));

        [NotMapped]
        public string StatusDisplay
        {
            get
            {
                return Status switch
                {
                    PaymentStatus.Scheduled => "Dijadwalkan",
                    PaymentStatus.Processed => "Diproses",
                    PaymentStatus.Confirmed => "Dikonfirmasi",
                    PaymentStatus.Failed => "Gagal",
                    PaymentStatus.Disputed => "Disengketakan",
                    _ => "Unknown"
                };
            }
        }

        [NotMapped]
        public string PaymentMethodDisplay
        {
            get
            {
                return PaymentMethod switch
                {
                    PaymentMethod.BankTransfer => "Transfer Bank",
                    PaymentMethod.Check => "Cek",
                    PaymentMethod.Cash => "Tunai",
                    PaymentMethod.CreditCard => "Kartu Kredit",
                    PaymentMethod.DigitalPayment => "Pembayaran Digital",
                    _ => "Unknown"
                };
            }
        }

        [NotMapped]
        public bool IsScheduled => Status == PaymentStatus.Scheduled;

        [NotMapped]
        public bool IsProcessed => Status == PaymentStatus.Processed;

        [NotMapped]
        public bool IsConfirmed => Status == PaymentStatus.Confirmed;

        [NotMapped]
        public bool IsFailed => Status == PaymentStatus.Failed;

        [NotMapped]
        public bool IsDisputed => Status == PaymentStatus.Disputed;

        [NotMapped]
        public bool CanEdit => Status == PaymentStatus.Scheduled;

        [NotMapped]
        public bool CanProcess => Status == PaymentStatus.Scheduled;

        [NotMapped]
        public bool CanConfirm => Status == PaymentStatus.Processed;

        [NotMapped]
        public bool CanCancel => Status == PaymentStatus.Scheduled || Status == PaymentStatus.Processed;

        [NotMapped]
        public bool RequiresApproval => Amount >= 25000000; // 25M IDR threshold for payment approval

        [NotMapped]
        public bool IsOverdue
        {
            get
            {
                if (Status != PaymentStatus.Scheduled) return false;
                return PaymentDate < DateTime.UtcNow.Date;
            }
        }

        [NotMapped]
        public int DaysOverdue
        {
            get
            {
                if (!IsOverdue) return 0;
                return (DateTime.UtcNow.Date - PaymentDate.Date).Days;
            }
        }

        [NotMapped]
        public int DaysUntilPayment
        {
            get
            {
                if (Status != PaymentStatus.Scheduled) return 0;
                var days = (PaymentDate.Date - DateTime.UtcNow.Date).Days;
                return Math.Max(0, days);
            }
        }

        [NotMapped]
        public bool IsDueToday => Status == PaymentStatus.Scheduled && PaymentDate.Date == DateTime.UtcNow.Date;

        [NotMapped]
        public bool IsDueSoon => Status == PaymentStatus.Scheduled && DaysUntilPayment <= 3 && DaysUntilPayment > 0;

        [NotMapped]
        public string PaymentReference => OurPaymentReference ?? TransferReference ?? CheckNumber ?? "";

        [NotMapped]
        public string ProcessingStatus
        {
            get
            {
                if (IsOverdue) return "Terlambat";
                if (IsDueToday) return "Jatuh Tempo Hari Ini";
                if (IsDueSoon) return "Jatuh Tempo Segera";
                return StatusDisplay;
            }
        }

        [NotMapped]
        public bool HasConfirmation => !string.IsNullOrEmpty(SupplierAckReference) || ConfirmedAt.HasValue;

        [NotMapped]
        public TimeSpan? ProcessingTime
        {
            get
            {
                if (Status == PaymentStatus.Scheduled) return null;
                return UpdatedAt - CreatedAt;
            }
        }
    }
}