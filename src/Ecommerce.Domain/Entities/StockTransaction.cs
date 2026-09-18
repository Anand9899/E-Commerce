using Ecommerce.Domain.Common;
using Ecommerce.Domain.Enums;

namespace Ecommerce.Domain.Entities
{
    public class StockTransaction : BaseEntity
    {
        public int ProductId { get; set; }
        public virtual Product Product { get; set; } = null!;

        public int? ProductVariantId { get; set; }
        public virtual ProductVariant? ProductVariant { get; set; }

        public int QuantityChange { get; set; } // +10 or -2
        public int RemainingStock { get; set; }
        public StockChangeType ChangeType { get; set; }
        public string? Reference { get; set; } // e.g., "Order ORD-202609-0001" or "Admin Manual Adjustment"
        public string? ChangedBy { get; set; }
    }
}
