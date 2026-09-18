using System;
using System.Collections.Generic;
using Ecommerce.Domain.Common;
using Ecommerce.Domain.Enums;

namespace Ecommerce.Domain.Entities
{
    public class Order : BaseEntity
    {
        public string OrderNumber { get; set; } = string.Empty; // e.g., ORD-202609-0001
        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser User { get; set; } = null!;

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; } = 0;
        public decimal ShippingFee { get; set; } = 0;
        public decimal TaxAmount { get; set; } = 0;
        public decimal GrandTotal { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CashOnDelivery;

        public string? CouponCode { get; set; }
        public string? TrackingNumber { get; set; }
        public string? CourierPartner { get; set; } // e.g., "BlueDart Express", "Delhivery", "DTDC", "FedEx", "Ecom Express", "India Post"
        public DateTime? EstimatedDeliveryDate { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? OutForDeliveryAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public string? CurrentLocation { get; set; } // e.g., "Gurugram Logistics Hub, Haryana"
        public decimal WalletAmountUsed { get; set; } = 0; // Amount deducted from customer wallet
        public int LoyaltyPointsEarned { get; set; } = 0; // Reward points credited on order
        public string? CustomerNotes { get; set; }

        // Return & Refund Details
        public string? ReturnReason { get; set; }
        public string? ReturnComments { get; set; }
        public string? ReturnRefundPaymentDetails { get; set; } // UPI ID or Bank account for COD refund
        public DateTime? ReturnRequestedAt { get; set; }
        public DateTime? ReturnActionAt { get; set; }
        public decimal? RefundAmount { get; set; }
        public string? RefundReferenceId { get; set; }
        public string? RefundAdminNotes { get; set; }

        // Shipping Address Snapshot (immutable)
        public string ShippingFullName { get; set; } = string.Empty;
        public string ShippingPhone { get; set; } = string.Empty;
        public string ShippingAddressLine1 { get; set; } = string.Empty;
        public string? ShippingAddressLine2 { get; set; }
        public string ShippingCity { get; set; } = string.Empty;
        public string ShippingState { get; set; } = string.Empty;
        public string ShippingPostalCode { get; set; } = string.Empty;
        public string ShippingCountry { get; set; } = "India";

        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public virtual ICollection<OrderStatusHistory> StatusHistories { get; set; } = new List<OrderStatusHistory>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
