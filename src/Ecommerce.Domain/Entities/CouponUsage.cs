using System;
using Ecommerce.Domain.Common;

namespace Ecommerce.Domain.Entities
{
    public class CouponUsage : BaseEntity
    {
        public int CouponId { get; set; }
        public virtual Coupon Coupon { get; set; } = null!;

        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser User { get; set; } = null!;

        public int OrderId { get; set; }
        public virtual Order Order { get; set; } = null!;

        public decimal DiscountAmount { get; set; }
        public DateTime UsedAt { get; set; } = DateTime.UtcNow;
    }
}
