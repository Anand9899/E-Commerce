using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Ecommerce.Domain.Enums;

namespace Ecommerce.Application.DTOs
{
    public class WalletSummaryVM
    {
        public decimal WalletBalance { get; set; }
        public int LoyaltyPoints { get; set; }
        public decimal PointsCashValue => LoyaltyPoints * 1.0m; // 1 pt = ₹1
        public decimal TotalEarnedCashback { get; set; }
        public decimal TotalSpentFromWallet { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string FullName => CustomerName;
        public string CustomerEmail { get; set; } = string.Empty;
        public DateTime MemberSince { get; set; } = DateTime.UtcNow;

        public List<WalletTransactionItemVM> Transactions { get; set; } = new List<WalletTransactionItemVM>();
    }

    public class WalletTransactionItemVM
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public WalletTransactionType Type { get; set; }
        public bool IsCredit => Type == WalletTransactionType.Credit;
        public string Description { get; set; } = string.Empty;
        public decimal BalanceAfter { get; set; }
        public int? OrderId { get; set; }
        public string? OrderNumber { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class TopUpWalletDto
    {
        [Required(ErrorMessage = "Top-up amount is required.")]
        [Range(10, 50000, ErrorMessage = "Top-up amount must be between ₹10 and ₹50,000.")]
        public decimal Amount { get; set; }

        public string PaymentMethod { get; set; } = "UPI"; // UPI, Cards, NetBanking
    }

    public class RedeemPointsDto
    {
        [Required(ErrorMessage = "Points amount is required.")]
        [Range(10, 100000, ErrorMessage = "Minimum 10 points required to convert to wallet cash.")]
        public int Points { get; set; }
    }
}
