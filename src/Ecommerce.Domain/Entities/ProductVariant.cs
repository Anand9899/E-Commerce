using Ecommerce.Domain.Common;

namespace Ecommerce.Domain.Entities
{
    public class ProductVariant : BaseEntity
    {
        public int ProductId { get; set; }
        public virtual Product Product { get; set; } = null!;
        public string VariantName { get; set; } = string.Empty; // e.g. "Size: M, Color: Blue"
        public string SKU { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; } = 0;
        public string? AttributesJson { get; set; } // {"Size": "M", "Color": "Blue"}
        public bool IsActive { get; set; } = true;
    }
}
