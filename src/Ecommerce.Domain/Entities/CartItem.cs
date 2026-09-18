using Ecommerce.Domain.Common;

namespace Ecommerce.Domain.Entities
{
    public class CartItem : BaseEntity
    {
        public string? UserId { get; set; }
        public virtual ApplicationUser? User { get; set; }
        public string? GuestSessionId { get; set; }

        public int ProductId { get; set; }
        public virtual Product Product { get; set; } = null!;

        public int? ProductVariantId { get; set; }
        public virtual ProductVariant? ProductVariant { get; set; }

        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
    }
}
