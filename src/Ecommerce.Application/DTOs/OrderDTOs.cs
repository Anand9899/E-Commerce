using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Ecommerce.Domain.Enums;

namespace Ecommerce.Application.DTOs
{
    public class CheckoutVM
    {
        public CartVM Cart { get; set; } = new CartVM();
        public List<AddressVM> SavedAddresses { get; set; } = new List<AddressVM>();
        public int? SelectedAddressId { get; set; }

        [Required(ErrorMessage = "Full Name is required")]
        public string ShippingFullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone Number is required")]
        [Phone]
        public string ShippingPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Street Address is required")]
        public string ShippingAddressLine1 { get; set; } = string.Empty;
        public string? ShippingAddressLine2 { get; set; }

        [Required(ErrorMessage = "City is required")]
        public string ShippingCity { get; set; } = string.Empty;

        [Required(ErrorMessage = "State is required")]
        public string ShippingState { get; set; } = string.Empty;

        [Required(ErrorMessage = "Postal Code is required")]
        public string ShippingPostalCode { get; set; } = string.Empty;

        public string ShippingCountry { get; set; } = "India";

        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.UPI;
        public string? UpiTransactionId { get; set; }
        public string? PaymentTransactionId { get; set; }
        public string? PaymentGatewayResponse { get; set; }
        public string? CustomerNotes { get; set; }
        public bool SaveAddressForFuture { get; set; } = false;

        // Direct 1-Click Buy Now (Independent of regular cart)
        public int? DirectProductId { get; set; }
        public int? DirectVariantId { get; set; }
        public int? DirectQuantity { get; set; }

        // Store Wallet & Loyalty Points
        public decimal AvailableWalletBalance { get; set; }
        public int AvailableLoyaltyPoints { get; set; }
        public bool UseWalletBalance { get; set; } = false;
        public decimal WalletAmountToDeduct { get; set; } = 0;
    }

    public class OrderListVM
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal GrandTotal { get; set; }
        public OrderStatus Status { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string? TransactionId { get; set; }
        public int TotalItems { get; set; }
        public string FirstProductImage { get; set; } = string.Empty;
        public string? TrackingNumber { get; set; }
        public string? CourierPartner { get; set; }
        public DateTime? EstimatedDeliveryDate { get; set; }
    }

    public class OrderDetailVM
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal GrandTotal { get; set; }
        public OrderStatus Status { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string? TransactionId { get; set; }
        public string? CouponCode { get; set; }
        public string? TrackingNumber { get; set; }
        public string? CourierPartner { get; set; }
        public DateTime? EstimatedDeliveryDate { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? OutForDeliveryAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public string? CurrentLocation { get; set; }
        public decimal WalletAmountUsed { get; set; }
        public int LoyaltyPointsEarned { get; set; }
        public string? CustomerNotes { get; set; }

        // Return & Refund Tracking
        public string? ReturnReason { get; set; }
        public string? ReturnComments { get; set; }
        public string? ReturnRefundPaymentDetails { get; set; }
        public DateTime? ReturnRequestedAt { get; set; }
        public DateTime? ReturnActionAt { get; set; }
        public decimal? RefundAmount { get; set; }
        public string? RefundReferenceId { get; set; }
        public string? RefundAdminNotes { get; set; }
        public bool IsEligibleForReturn => Status == OrderStatus.Delivered && 
                                           (DateTime.UtcNow - OrderDate).TotalDays <= 7;

        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }

        public string ShippingFullName { get; set; } = string.Empty;
        public string ShippingPhone { get; set; } = string.Empty;
        public string ShippingAddressLine1 { get; set; } = string.Empty;
        public string? ShippingAddressLine2 { get; set; }
        public string ShippingCity { get; set; } = string.Empty;
        public string ShippingState { get; set; } = string.Empty;
        public string ShippingPostalCode { get; set; } = string.Empty;
        public string ShippingCountry { get; set; } = "India";

        public List<OrderItemVM> Items { get; set; } = new List<OrderItemVM>();
        public List<OrderStatusHistoryVM> StatusHistories { get; set; } = new List<OrderStatusHistoryVM>();
    }

    public class OrderItemVM
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? VariantName { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string? ProductImageUrl { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class OrderStatusHistoryVM
    {
        public OrderStatus Status { get; set; }
        public DateTime ChangedAt { get; set; }
        public string? Notes { get; set; }
        public string? ChangedBy { get; set; }
    }

    public class ReturnRequestDto
    {
        public int OrderId { get; set; }

        [Required(ErrorMessage = "Please select a reason for return.")]
        public string Reason { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please provide additional details about why you want to return this item.")]
        [StringLength(500, ErrorMessage = "Comments cannot exceed 500 characters.")]
        public string Comments { get; set; } = string.Empty;

        public string? RefundPaymentDetails { get; set; } // e.g., UPI ID / Bank IFSC & Acc Number for COD
    }

    public class ProcessReturnDto
    {
        public int OrderId { get; set; }

        [Required]
        public string Action { get; set; } = "Approve"; // "Approve", "Reject", "Refund"

        public decimal? RefundAmount { get; set; }
        public string? RefundReferenceId { get; set; }
        public string? AdminNotes { get; set; }
        public bool RestockInventory { get; set; } = true;
    }

    public class UpdateShipmentTrackingDto
    {
        public int OrderId { get; set; }
        public OrderStatus Status { get; set; }
        public string? TrackingNumber { get; set; }
        public string? CourierPartner { get; set; }
        public string? CurrentLocation { get; set; }
        public DateTime? EstimatedDeliveryDate { get; set; }
        public string? Notes { get; set; }
    }
}

