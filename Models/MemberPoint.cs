// Models/MemberPoint.cs - Sprint 2 Point History
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Berca_Backend.Models
{
    public class MemberPoint
    {
        public int Id { get; set; }

        // Foreign Key
        [Required]
        public int MemberId { get; set; }
        public virtual Member Member { get; set; } = null!;

        // Point Transaction Details
        [Required]
        public int Points { get; set; } // Positive for earning, negative for redemption

        public PointTransactionType Type { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        // Related Transaction (if applicable)
        public int? SaleId { get; set; }
        public virtual Sale? Sale { get; set; }

        [StringLength(50)]
        public string? ReferenceNumber { get; set; }

        // Point Rules Applied
        [Column(TypeName = "decimal(18,2)")]
        public decimal? TransactionAmount { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? PointRate { get; set; } // e.g., 1 point per 1000 rupiah

        // Expiry (untuk point yang bisa expired)
        public DateTime? ExpiryDate { get; set; }

        public bool IsExpired { get; set; } = false;

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }

        // Computed Properties
        [NotMapped]
        public bool IsEarning => Points > 0;

        [NotMapped]
        public bool IsRedemption => Points < 0;

        [NotMapped]
        public string TypeDisplay => Type switch
        {
            PointTransactionType.Purchase => "Pembelian",
            PointTransactionType.Redemption => "Penukaran",
            PointTransactionType.Bonus => "Bonus",
            PointTransactionType.Adjustment => "Penyesuaian",
            PointTransactionType.Expiry => "Kedaluwarsa",
            _ => "Lainnya"
        };
    }

    public enum PointTransactionType
    {
        Purchase = 0,      // Mendapat poin dari pembelian
        Redemption = 1,    // Menggunakan poin untuk diskon
        Bonus = 2,         // Bonus poin dari promo
        Adjustment = 3,    // Penyesuaian manual
        Expiry = 4,         // Poin kedaluwarsa
        Earn=0,
        Redeem=0,
    }
}