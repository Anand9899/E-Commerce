using System;
using System.Collections.Generic;
using Ecommerce.Domain.Common;
using Ecommerce.Domain.Enums;

namespace Ecommerce.Domain.Entities
{
    public class Coupon : BaseEntity
    {
        public string Code { get; set; } = string.Empty; // e.g., "SAVE20"
        public string? Description { get; set; }
        public DiscountType DiscountType { get; set; } = DiscountType.Percentage;
        public decimal DiscountValue { get; set; } // e.g., 10% or Rs. 100
        public decimal MinOrderAmount { get; set; } = 0;
        public decimal? MaxDiscountAmount { get; set; }
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime ExpiryDate { get; set; } = DateTime.UtcNow.AddMonths(1);
        public int? UsageLimit { get; set; }
        public int UsedCount { get; set; } = 0;
        public bool IsActive { get; set; } = true;

        public virtual ICollection<CouponUsage> CouponUsages { get; set; } = new List<CouponUsage>();
    }
}
