using System;

namespace Berca_Backend.DTOs
{
    /// <summary>
    /// Compact DTO for branch-specific product data optimized for performance
    /// Contains only essential fields for faster serialization/transfer
    /// </summary>
    public class BranchProductCompactDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public decimal SellPrice { get; set; }

        /// <summary>
        /// Branch-specific stock for this product
        /// </summary>
        public int Stock { get; set; }

        /// <summary>
        /// The specific branch ID this stock data is for
        /// </summary>
        public int BranchId { get; set; }

        /// <summary>
        /// Branch name for display purposes
        /// </summary>
        public string BranchName { get; set; } = string.Empty;

        public int MinStock { get; set; }
        public string Unit { get; set; } = string.Empty;
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }

        /// <summary>
        /// Stock status code: 0=Out, 1=Low, 2=Normal
        /// </summary>
        public byte Status => (byte)(Stock <= 0 ? 0 : Stock <= MinStock ? 1 : 2);
    }

    /// <summary>
    /// Paginated response for branch products
    /// </summary>
    public class BranchProductPagedResponse
    {
        public List<BranchProductCompactDto> Products { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasNextPage => Page < TotalPages;
        public bool HasPreviousPage => Page > 1;
    }
}