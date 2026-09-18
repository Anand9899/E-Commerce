using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Ecommerce.Application.DTOs
{
    public class CartVM
    {
        public List<CartItemVM> Items { get; set; } = new List<CartItemVM>();
        public decimal SubTotal => Items.Sum(x => x.TotalPrice);
        public decimal DiscountAmount { get; set; } = 0;
        public string? AppliedCouponCode { get; set; }
        public decimal ShippingFee => SubTotal > 999 || SubTotal == 0 ? 0 : 99; // Free shipping over ₹999
        public decimal TaxAmount => Math.Round((SubTotal - DiscountAmount) * 0.18m, 2); // 18% GST standard preview
        public decimal GrandTotal => Math.Max(0, SubTotal - DiscountAmount + ShippingFee + TaxAmount);
        public int TotalQuantity => Items.Sum(x => x.Quantity);
    }

    public class CartItemVM
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductSlug { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int? ProductVariantId { get; set; }
        public string? VariantName { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public int AvailableStock { get; set; }
        public decimal TotalPrice => UnitPrice * Quantity;
    }

    public class AddToCartDto
    {
        [Required]
        public int ProductId { get; set; }
        public int? ProductVariantId { get; set; }
        [Range(1, 100)]
        public int Quantity { get; set; } = 1;
    }
}
