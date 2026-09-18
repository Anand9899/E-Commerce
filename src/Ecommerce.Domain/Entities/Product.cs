using System.Collections.Generic;
using Ecommerce.Domain.Common;

namespace Ecommerce.Domain.Entities
{
    public class Product : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string? ShortDescription { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int StockQuantity { get; set; } = 0;
        public int LowStockThreshold { get; set; } = 5;
        public bool IsFeatured { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public double AverageRating { get; set; } = 0;
        public int TotalReviews { get; set; } = 0;

        // Foreign keys
        public int CategoryId { get; set; }
        public virtual Category Category { get; set; } = null!;

        public int? BrandId { get; set; }
        public virtual Brand? Brand { get; set; }

        // Navigation
        public virtual ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
        public virtual ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();
    }
}
