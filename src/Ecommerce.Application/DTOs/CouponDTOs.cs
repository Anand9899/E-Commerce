using System;
using Ecommerce.Domain.Enums;

namespace Ecommerce.Application.DTOs
{
    public class CouponVM
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DiscountType DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal MinOrderAmount { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsActive { get; set; }
    }
}
