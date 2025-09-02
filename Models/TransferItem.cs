using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Berca_Backend.Models
{
    /// <summary>
    /// Represents an individual item in a transfer request
    /// Required for frontend multi-branch inventory management
    /// </summary>
    public class TransferItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TransferRequestId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public int RequestedQuantity { get; set; }

        public int? ApprovedQuantity { get; set; }

        public int? TransferredQuantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        // Additional properties for AppDbContext compatibility
        [NotMapped]
        public int Quantity
        {
            get => RequestedQuantity;
            set => RequestedQuantity = value; // Add setter for EF compatibility
        }

        [NotMapped]
        public decimal UnitCost
        {
            get => UnitPrice;
            set => UnitPrice = value; // Add setter for EF compatibility
        }

        [NotMapped]
        public decimal TotalCost
        {
            get => TotalPrice;
            set => TotalPrice = value; // Add setter for EF compatibility
        }

        [MaxLength(500)]
        public string? Notes { get; set; }

        // Navigation properties
        public virtual TransferRequest TransferRequest { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;

        // Computed properties
        [NotMapped]
        public int ActualQuantity => TransferredQuantity ?? ApprovedQuantity ?? RequestedQuantity;

        [NotMapped]
        public decimal ActualTotalPrice => ActualQuantity * UnitPrice;

        [NotMapped]
        public bool IsFullyTransferred => TransferredQuantity.HasValue && 
                                         TransferredQuantity == (ApprovedQuantity ?? RequestedQuantity);

        [NotMapped]
        public bool IsPartiallyTransferred => TransferredQuantity.HasValue && 
                                             TransferredQuantity < (ApprovedQuantity ?? RequestedQuantity);

        [NotMapped]
        public int PendingQuantity => (ApprovedQuantity ?? RequestedQuantity) - (TransferredQuantity ?? 0);

        [NotMapped]
        public string StatusText
        {
            get
            {
                if (!TransferredQuantity.HasValue)
                {
                    return ApprovedQuantity.HasValue ? "Approved" : "Pending";
                }

                if (IsFullyTransferred)
                    return "Completed";

                if (IsPartiallyTransferred)
                    return "Partial";

                return TransferredQuantity == 0 ? "Not Started" : "In Progress";
            }
        }

        // Helper methods
        public void Approve(int approvedQuantity, string? notes = null)
        {
            if (approvedQuantity < 0)
                throw new ArgumentException("Approved quantity cannot be negative", nameof(approvedQuantity));

            if (approvedQuantity > RequestedQuantity)
                throw new ArgumentException("Approved quantity cannot exceed requested quantity", nameof(approvedQuantity));

            ApprovedQuantity = approvedQuantity;
            
            if (!string.IsNullOrEmpty(notes))
            {
                Notes = string.IsNullOrEmpty(Notes) ? notes : $"{Notes}; {notes}";
            }

            // Update total price based on approved quantity
            TotalPrice = ApprovedQuantity.Value * UnitPrice;
        }

        public void UpdateTransferredQuantity(int transferredQuantity, string? notes = null)
        {
            var maxAllowed = ApprovedQuantity ?? RequestedQuantity;
            
            if (transferredQuantity < 0)
                throw new ArgumentException("Transferred quantity cannot be negative", nameof(transferredQuantity));

            if (transferredQuantity > maxAllowed)
                throw new ArgumentException($"Transferred quantity ({transferredQuantity}) cannot exceed approved quantity ({maxAllowed})", nameof(transferredQuantity));

            TransferredQuantity = transferredQuantity;

            if (!string.IsNullOrEmpty(notes))
            {
                Notes = string.IsNullOrEmpty(Notes) ? notes : $"{Notes}; {notes}";
            }
        }

        public void RecalculateTotalPrice()
        {
            var quantity = ApprovedQuantity ?? RequestedQuantity;
            TotalPrice = quantity * UnitPrice;
        }

        public static TransferItem Create(int productId, int requestedQuantity, decimal unitPrice, string? notes = null)
        {
            if (requestedQuantity <= 0)
                throw new ArgumentException("Requested quantity must be greater than 0", nameof(requestedQuantity));

            if (unitPrice < 0)
                throw new ArgumentException("Unit price cannot be negative", nameof(unitPrice));

            return new TransferItem
            {
                ProductId = productId,
                RequestedQuantity = requestedQuantity,
                UnitPrice = unitPrice,
                TotalPrice = requestedQuantity * unitPrice,
                Notes = notes
            };
        }
    }
}