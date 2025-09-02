using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Berca_Backend.Models
{
    /// <summary>
    /// Represents an inventory transfer request between branches
    /// Required for frontend multi-branch inventory management
    /// </summary>
    public class TransferRequest
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string TransferNumber { get; set; } = string.Empty;

        [Required]
        public int SourceBranchId { get; set; }

        [Required]
        public int TargetBranchId { get; set; }

        [Required]
        public TransferStatus Status { get; set; } = TransferStatus.Pending;

        [Required]
        public TransferPriority Priority { get; set; } = TransferPriority.Normal;

        [Required, MaxLength(500)]
        public string Reason { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public int TotalItems { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalValue { get; set; }

        [Required]
        public int RequestedBy { get; set; }

        [Required]
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        public int? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Branch SourceBranch { get; set; } = null!;
        public virtual Branch TargetBranch { get; set; } = null!;
        public virtual User RequestedByUser { get; set; } = null!;
        public virtual User? ApprovedByUser { get; set; }
        public virtual ICollection<TransferItem> Items { get; set; } = new List<TransferItem>();

        // Computed properties
        [NotMapped]
        public string StatusText => Status switch
        {
            TransferStatus.Pending => "Pending Approval",
            TransferStatus.Approved => "Approved",
            TransferStatus.Rejected => "Rejected",
            TransferStatus.InTransit => "In Transit",
            TransferStatus.Completed => "Completed",
            TransferStatus.Cancelled => "Cancelled",
            _ => "Unknown"
        };

        [NotMapped]
        public string PriorityText => Priority switch
        {
            TransferPriority.Low => "Low",
            TransferPriority.Normal => "Normal",
            TransferPriority.High => "High",
            TransferPriority.Emergency => "Emergency",
            _ => "Unknown"
        };

        [NotMapped]
        public bool CanBeApproved => Status == TransferStatus.Pending;

        [NotMapped]
        public bool CanBeCompleted => Status == TransferStatus.Approved || Status == TransferStatus.InTransit;

        [NotMapped]
        public bool CanBeCancelled => Status is TransferStatus.Pending or TransferStatus.Approved;

        // Helper methods
        public void Approve(int approvedBy, string? notes = null)
        {
            if (!CanBeApproved)
                throw new InvalidOperationException($"Cannot approve transfer in {Status} status");

            Status = TransferStatus.Approved;
            ApprovedBy = approvedBy;
            ApprovedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(notes))
            {
                Notes = string.IsNullOrEmpty(Notes) ? notes : $"{Notes}\n{notes}";
            }
        }

        public void Reject(int rejectedBy, string reason)
        {
            if (Status != TransferStatus.Pending)
                throw new InvalidOperationException($"Cannot reject transfer in {Status} status");

            Status = TransferStatus.Rejected;
            ApprovedBy = rejectedBy;
            ApprovedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            Notes = string.IsNullOrEmpty(Notes) ? reason : $"{Notes}\nRejected: {reason}";
        }

        public void MarkInTransit()
        {
            if (Status != TransferStatus.Approved)
                throw new InvalidOperationException($"Cannot mark transfer as in transit from {Status} status");

            Status = TransferStatus.InTransit;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Complete()
        {
            if (!CanBeCompleted)
                throw new InvalidOperationException($"Cannot complete transfer in {Status} status");

            Status = TransferStatus.Completed;
            CompletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Cancel(string reason)
        {
            if (!CanBeCancelled)
                throw new InvalidOperationException($"Cannot cancel transfer in {Status} status");

            Status = TransferStatus.Cancelled;
            UpdatedAt = DateTime.UtcNow;
            Notes = string.IsNullOrEmpty(Notes) ? $"Cancelled: {reason}" : $"{Notes}\nCancelled: {reason}";
        }

        public void UpdateTotalValues()
        {
            TotalItems = Items.Sum(i => i.RequestedQuantity);
            TotalValue = Items.Sum(i => i.TotalPrice);
            UpdatedAt = DateTime.UtcNow;
        }

        public static string GenerateTransferNumber()
        {
            return $"TRF-{DateTime.UtcNow:yyyy}-{DateTime.UtcNow.Ticks.ToString()[^6..]}";
        }
    }
}