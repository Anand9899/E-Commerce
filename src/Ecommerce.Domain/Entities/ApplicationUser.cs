using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace Ecommerce.Domain.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? Gender { get; set; }
        public string? PlainPassword { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; } = true;

        // Store Wallet & Loyalty Rewards
        public decimal WalletBalance { get; set; } = 500; // Welcome ₹500 store wallet credit
        public int LoyaltyPoints { get; set; } = 100; // Welcome 100 reward points

        // Navigation properties
        public virtual ICollection<Address> Addresses { get; set; } = new List<Address>();
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public virtual ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
        public virtual ICollection<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();
    }
}
