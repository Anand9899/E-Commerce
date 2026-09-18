using Ecommerce.Domain.Common;

namespace Ecommerce.Domain.Entities
{
    public class Review : BaseEntity
    {
        public int ProductId { get; set; }
        public virtual Product Product { get; set; } = null!;

        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser User { get; set; } = null!;

        public int Rating { get; set; } // 1 to 5
        public string Title { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public bool IsVerifiedPurchase { get; set; } = false;
        public bool IsApproved { get; set; } = true;
    }
}
