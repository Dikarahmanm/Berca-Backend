using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Berca_Backend.Models
{
    /// <summary>
    /// Model for tracking all credit-related transactions for members
    /// Supports both debt accumulation (credit sales) and debt reduction (payments)
    /// </summary>
    public class MemberCreditTransaction
    {
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to Member who owns this transaction
        /// </summary>
        [Required]
        public int MemberId { get; set; }

        /// <summary>
        /// Type of credit transaction (sale, payment, interest, penalty, adjustment)
        /// </summary>
        [Required]
        public CreditTransactionType Type { get; set; }

        /// <summary>
        /// Transaction amount in Indonesian Rupiah (IDR)
        /// Positive for debt increases (sales, interest, penalties)
        /// Negative for debt reductions (payments, adjustments)
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Date and time when transaction occurred (Jakarta timezone)
        /// </summary>
        [Required]
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Payment due date for credit sales (null for payments)
        /// </summary>
        public DateTime? DueDate { get; set; }

        /// <summary>
        /// Human-readable description of the transaction
        /// </summary>
        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Reference to original sale ID, payment reference, etc.
        /// </summary>
        [StringLength(100)]
        public string? ReferenceNumber { get; set; }

        /// <summary>
        /// Current status of this transaction
        /// </summary>
        [Required]
        public CreditTransactionStatus Status { get; set; } = CreditTransactionStatus.Completed;

        /// <summary>
        /// ID of user who created this transaction
        /// </summary>
        [Required]
        public int CreatedBy { get; set; }

        /// <summary>
        /// Branch where transaction was processed (for multi-branch operations)
        /// </summary>
        public int? BranchId { get; set; }

        /// <summary>
        /// Additional notes or remarks about the transaction
        /// </summary>
        [StringLength(1000)]
        public string? Notes { get; set; }

        // Audit Properties
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        
        /// <summary>
        /// Member who owns this credit transaction
        /// </summary>
        public virtual Member Member { get; set; } = null!;

        /// <summary>
        /// User who created this transaction (for audit trail)
        /// </summary>
        public virtual User CreatedByUser { get; set; } = null!;

        /// <summary>
        /// Branch where transaction occurred (optional for multi-branch)
        /// </summary>
        public virtual Branch? Branch { get; set; }

        // Computed Properties

        /// <summary>
        /// Whether this transaction increases member debt
        /// </summary>
        [NotMapped]
        public bool IncreasesDebt => Type == CreditTransactionType.CreditSale || 
                                   Type == CreditTransactionType.Interest || 
                                   Type == CreditTransactionType.Penalty;

        /// <summary>
        /// Whether this transaction reduces member debt
        /// </summary>
        [NotMapped]
        public bool ReducesDebt => Type == CreditTransactionType.Payment || 
                                 (Type == CreditTransactionType.Adjustment && Amount < 0);

        /// <summary>
        /// Days until payment is due (for credit sales)
        /// </summary>
        [NotMapped]
        public int? DaysUntilDue
        {
            get
            {
                if (DueDate == null || Type != CreditTransactionType.CreditSale) return null;
                return (DueDate.Value.Date - DateTime.UtcNow.Date).Days;
            }
        }

        /// <summary>
        /// Whether this transaction is overdue
        /// </summary>
        [NotMapped]
        public bool IsOverdue
        {
            get
            {
                if (DueDate == null || Type != CreditTransactionType.CreditSale) return false;
                return DateTime.UtcNow.Date > DueDate.Value.Date;
            }
        }
    }
}