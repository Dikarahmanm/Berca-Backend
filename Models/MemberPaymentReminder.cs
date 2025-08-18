using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Berca_Backend.Models
{
    /// <summary>
    /// Model for tracking payment reminders sent to members with outstanding debt
    /// Supports automated collection processes and follow-up tracking
    /// </summary>
    public class MemberPaymentReminder
    {
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to Member who received the reminder
        /// </summary>
        [Required]
        public int MemberId { get; set; }

        /// <summary>
        /// Type of reminder communication sent
        /// </summary>
        [Required]
        public ReminderType ReminderType { get; set; }

        /// <summary>
        /// Date and time when reminder was sent (Jakarta timezone)
        /// </summary>
        [Required]
        public DateTime ReminderDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Total amount due at time of reminder (IDR)
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DueAmount { get; set; }

        /// <summary>
        /// Number of days payment was overdue when reminder sent
        /// </summary>
        [Required]
        public int DaysOverdue { get; set; }

        /// <summary>
        /// Reminder message content sent to member
        /// </summary>
        [Required]
        [StringLength(1000)]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Current status of reminder delivery and response
        /// </summary>
        [Required]
        public ReminderStatus Status { get; set; } = ReminderStatus.Sent;

        /// <summary>
        /// Date when member responded to reminder (if any)
        /// </summary>
        public DateTime? ResponseDate { get; set; }

        /// <summary>
        /// Amount paid by member after receiving reminder (if any)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? ResponseAmount { get; set; }

        /// <summary>
        /// When next reminder should be sent (if needed)
        /// </summary>
        public DateTime? NextReminderDate { get; set; }

        /// <summary>
        /// SMS phone number or email address where reminder was sent
        /// </summary>
        [StringLength(100)]
        public string? ContactMethod { get; set; }

        /// <summary>
        /// Priority level of this reminder (escalation tracking)
        /// </summary>
        [Required]
        public ReminderPriority Priority { get; set; } = ReminderPriority.Normal;

        /// <summary>
        /// User ID who initiated the reminder (manual vs automated)
        /// </summary>
        [Required]
        public int CreatedBy { get; set; }

        /// <summary>
        /// Branch responsible for this collection (for multi-branch operations)
        /// </summary>
        public int? BranchId { get; set; }

        /// <summary>
        /// Additional notes about reminder or member response
        /// </summary>
        [StringLength(500)]
        public string? Notes { get; set; }

        // Audit Properties
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties

        /// <summary>
        /// Member who received this reminder
        /// </summary>
        public virtual Member Member { get; set; } = null!;

        /// <summary>
        /// User who created/sent this reminder
        /// </summary>
        public virtual User CreatedByUser { get; set; } = null!;

        /// <summary>
        /// Branch responsible for collections (optional)
        /// </summary>
        public virtual Branch? Branch { get; set; }

        // Computed Properties

        /// <summary>
        /// Whether member has responded to this reminder
        /// </summary>
        [NotMapped]
        public bool HasResponse => ResponseDate.HasValue;

        /// <summary>
        /// Days since reminder was sent
        /// </summary>
        [NotMapped]
        public int DaysSinceReminder => (DateTime.UtcNow.Date - ReminderDate.Date).Days;

        /// <summary>
        /// Whether it's time to send next reminder
        /// </summary>
        [NotMapped]
        public bool IsNextReminderDue
        {
            get
            {
                if (NextReminderDate == null || HasResponse) return false;
                return DateTime.UtcNow.Date >= NextReminderDate.Value.Date;
            }
        }

        /// <summary>
        /// Effectiveness of reminder (did member pay after receiving it?)
        /// </summary>
        [NotMapped]
        public bool WasEffective => ResponseAmount.HasValue && ResponseAmount.Value > 0;
    }

    // ==================== REMINDER SYSTEM ENUMS ==================== //

    /// <summary>
    /// Types of payment reminder communications
    /// </summary>
    public enum ReminderType
    {
        /// <summary>
        /// SMS text message reminder
        /// </summary>
        SMS = 0,

        /// <summary>
        /// Email reminder notification
        /// </summary>
        Email = 1,

        /// <summary>
        /// Phone call reminder
        /// </summary>
        Call = 2,

        /// <summary>
        /// In-person reminder at store
        /// </summary>
        InPerson = 3,

        /// <summary>
        /// WhatsApp message reminder
        /// </summary>
        WhatsApp = 4,

        /// <summary>
        /// Written notice/letter
        /// </summary>
        Letter = 5
    }

    /// <summary>
    /// Status of reminder delivery and member response
    /// </summary>
    public enum ReminderStatus
    {
        /// <summary>
        /// Reminder has been sent
        /// </summary>
        Sent = 0,

        /// <summary>
        /// Reminder delivered successfully
        /// </summary>
        Delivered = 1,

        /// <summary>
        /// Reminder delivery failed
        /// </summary>
        Failed = 2,

        /// <summary>
        /// Member responded to reminder
        /// </summary>
        Responded = 3,

        /// <summary>
        /// Reminder scheduled but not yet sent
        /// </summary>
        Scheduled = 4
    }

    /// <summary>
    /// Priority levels for payment reminders (escalation)
    /// </summary>
    public enum ReminderPriority
    {
        /// <summary>
        /// Low priority - gentle reminder
        /// </summary>
        Low = 0,

        /// <summary>
        /// Normal priority - standard reminder
        /// </summary>
        Normal = 1,

        /// <summary>
        /// High priority - urgent reminder
        /// </summary>
        High = 2,

        /// <summary>
        /// Critical priority - final notice before collections
        /// </summary>
        Critical = 3
    }
}