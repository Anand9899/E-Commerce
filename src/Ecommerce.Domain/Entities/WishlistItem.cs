using Ecommerce.Domain.Common;

namespace Ecommerce.Domain.Entities
{
    public class WishlistItem : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser User { get; set; } = null!;

        public int ProductId { get; set; }
        public virtual Product Product { get; set; } = null!;
    }
}
