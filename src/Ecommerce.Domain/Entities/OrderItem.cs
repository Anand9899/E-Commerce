using Ecommerce.Domain.Common;

namespace Ecommerce.Domain.Entities
{
    public class OrderItem : BaseEntity
    {
        public int OrderId { get; set; }
        public virtual Order Order { get; set; } = null!;

        public int ProductId { get; set; }
        public virtual Product Product { get; set; } = null!;

        public int? ProductVariantId { get; set; }
        public virtual ProductVariant? ProductVariant { get; set; }

        public string ProductName { get; set; } = string.Empty;
        public string? VariantName { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string? ProductImageUrl { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
