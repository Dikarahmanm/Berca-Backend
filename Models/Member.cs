// Models/Member.cs - Sprint 2 Customer Loyalty
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Berca_Backend.Models
{
    public class Member
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [StringLength(100)]
        [EmailAddress]
        public string? Email { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [StringLength(10)]
        public string? Gender { get; set; }

        // Membership Info
        [Required]
        [StringLength(20)]
        public string MemberNumber { get; set; } = string.Empty;

        public MembershipTier Tier { get; set; } = MembershipTier.Bronze;

        public DateTime JoinDate { get; set; } = DateTime.UtcNow; // ✅ Fixed: was JoinedDate

        public bool IsActive { get; set; } = true;

        // Points System
        [Range(0, int.MaxValue)]
        public int TotalPoints { get; set; } = 0;

        [Range(0, int.MaxValue)]
        public int UsedPoints { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalSpent { get; set; } = 0;

        public int TotalTransactions { get; set; } = 0;

        public DateTime? LastTransactionDate { get; set; }

        // ==================== CREDIT SYSTEM ==================== //
        
        /// <summary>
        /// Maximum credit amount allowed for this member (IDR)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 100000000, ErrorMessage = "Credit limit must be between 0 and 100,000,000 IDR")]
        public decimal CreditLimit { get; set; } = 0;

        /// <summary>
        /// Current outstanding debt balance (IDR)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Current debt cannot be negative")]
        public decimal CurrentDebt { get; set; } = 0;

        /// <summary>
        /// Days allowed to pay debt (payment terms)
        /// </summary>
        [Range(1, 90, ErrorMessage = "Payment terms must be between 1 and 90 days")]
        public int PaymentTerms { get; set; } = 30;

        /// <summary>
        /// Current credit standing status
        /// </summary>
        public CreditStatus CreditStatus { get; set; } = CreditStatus.Good;

        /// <summary>
        /// Date of last debt payment
        /// </summary>
        public DateTime? LastPaymentDate { get; set; }

        /// <summary>
        /// Next payment due date for outstanding debt
        /// </summary>
        public DateTime? NextPaymentDueDate { get; set; }

        // ==================== RISK ASSESSMENT ==================== //
        
        /// <summary>
        /// Count of late payments (risk factor)
        /// </summary>
        [Range(0, int.MaxValue)]
        public int PaymentDelays { get; set; } = 0;

        /// <summary>
        /// Total debt accumulated over member lifetime (IDR)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal LifetimeDebt { get; set; } = 0;

        /// <summary>
        /// Member credit score (300-850 like FICO)
        /// </summary>
        [Range(300, 850)]
        public int CreditScore { get; set; } = 600; // Default to "Fair" credit

        // Navigation Properties
        public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
        public virtual ICollection<MemberPoint> MemberPoints { get; set; } = new List<MemberPoint>();
        public virtual ICollection<MemberCreditTransaction> CreditTransactions { get; set; } = new List<MemberCreditTransaction>();
        public virtual ICollection<MemberPaymentReminder> PaymentReminders { get; set; } = new List<MemberPaymentReminder>();

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }

        // Computed Properties
        [NotMapped]
        public int AvailablePoints => TotalPoints - UsedPoints;

        [NotMapped]
        public decimal AverageTransactionValue => TotalTransactions > 0 ? TotalSpent / TotalTransactions : 0;

        /// <summary>
        /// Available credit for new purchases (CreditLimit - CurrentDebt)
        /// </summary>
        [NotMapped]
        public decimal AvailableCredit => CreditLimit - CurrentDebt;

        /// <summary>
        /// Credit utilization percentage (CurrentDebt / CreditLimit * 100)
        /// </summary>
        [NotMapped]
        public decimal CreditUtilization => CreditLimit > 0 ? (CurrentDebt / CreditLimit) * 100 : 0;

        /// <summary>
        /// Payment success rate percentage
        /// </summary>
        [NotMapped]
        public decimal PaymentSuccessRate
        {
            get
            {
                var totalPaymentsDue = CreditTransactions?.Count(t => t.Type == CreditTransactionType.CreditSale) ?? 0;
                if (totalPaymentsDue == 0) return 100; // No credit history = perfect score
                return totalPaymentsDue > 0 ? ((decimal)(totalPaymentsDue - PaymentDelays) / totalPaymentsDue) * 100 : 100;
            }
        }

        /// <summary>
        /// Days overdue for current debt (if any)
        /// </summary>
        [NotMapped]
        public int DaysOverdue
        {
            get
            {
                if (NextPaymentDueDate == null || CurrentDebt <= 0) return 0;
                var daysOver = (DateTime.UtcNow.Date - NextPaymentDueDate.Value.Date).Days;
                return Math.Max(0, daysOver);
            }
        }
    }

    public enum MembershipTier
    {
        Bronze = 0,
        Silver = 1,
        Gold = 2,
        Platinum = 3
    }
    public enum MemberPointType
    {
        Earned = 0,
        Redeemed = 1,
        Expired = 2,
        Bonus = 3
    }

    // ==================== CREDIT SYSTEM ENUMS ==================== //

    /// <summary>
    /// Credit status levels for risk management
    /// </summary>
    public enum CreditStatus
    {
        /// <summary>
        /// Good standing - payments on time, credit available
        /// </summary>
        Good = 0,

        /// <summary>
        /// Warning - approaching credit limit or minor payment delays
        /// </summary>
        Warning = 1,

        /// <summary>
        /// Overdue - payment past due date, credit may be restricted
        /// </summary>
        Overdue = 2,

        /// <summary>
        /// Suspended - credit privileges suspended due to poor payment history
        /// </summary>
        Suspended = 3,

        /// <summary>
        /// Blacklisted - permanent credit ban due to fraud or severe default
        /// </summary>
        Blacklisted = 4
    }

    /// <summary>
    /// Types of credit transactions for tracking debt and payments
    /// </summary>
    public enum CreditTransactionType
    {
        /// <summary>
        /// Credit sale - member purchased on credit
        /// </summary>
        CreditSale = 0,

        /// <summary>
        /// Payment - member paid down debt
        /// </summary>
        Payment = 1,

        /// <summary>
        /// Interest charge for late payment
        /// </summary>
        Interest = 2,

        /// <summary>
        /// Penalty fee for overdue account
        /// </summary>
        Penalty = 3,

        /// <summary>
        /// Manual adjustment by manager
        /// </summary>
        Adjustment = 4
    }

    /// <summary>
    /// Status of credit transactions
    /// </summary>
    public enum CreditTransactionStatus
    {
        /// <summary>
        /// Transaction pending processing
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Transaction completed successfully
        /// </summary>
        Completed = 1,

        /// <summary>
        /// Transaction failed
        /// </summary>
        Failed = 2,

        /// <summary>
        /// Transaction disputed by member
        /// </summary>
        Disputed = 3
    }
}