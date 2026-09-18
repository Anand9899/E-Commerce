using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enums;
using Ecommerce.Infrastructure.Data;

namespace Ecommerce.Infrastructure.Services
{
    public class WalletService : IWalletService
    {
        private readonly ApplicationDbContext _context;

        public WalletService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<WalletSummaryVM> GetWalletSummaryAsync(string userId)
        {
            var user = await _context.Users
                .Include(u => u.WalletTransactions)
                    .ThenInclude(t => t.Order)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return new WalletSummaryVM();
            }

            var transactions = user.WalletTransactions
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new WalletTransactionItemVM
                {
                    Id = t.Id,
                    Amount = t.Amount,
                    Type = t.Type,
                    Description = t.Description,
                    BalanceAfter = t.BalanceAfter,
                    OrderId = t.OrderId,
                    OrderNumber = t.Order?.OrderNumber,
                    CreatedAt = t.CreatedAt
                })
                .ToList();

            var totalEarned = transactions
                .Where(t => t.Type == WalletTransactionType.Credit)
                .Sum(t => t.Amount);

            var totalSpent = transactions
                .Where(t => t.Type == WalletTransactionType.Debit)
                .Sum(t => t.Amount);

            return new WalletSummaryVM
            {
                WalletBalance = user.WalletBalance,
                LoyaltyPoints = user.LoyaltyPoints,
                TotalEarnedCashback = totalEarned,
                TotalSpentFromWallet = totalSpent,
                CustomerName = user.FullName ?? "Customer",
                CustomerEmail = user.Email ?? string.Empty,
                MemberSince = user.CreatedAt,
                Transactions = transactions
            };
        }

        public async Task<bool> CreditWalletAsync(string userId, decimal amount, string description, int? orderId = null)
        {
            if (amount <= 0) return false;

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            user.WalletBalance += amount;

            var transaction = new WalletTransaction
            {
                UserId = userId,
                Amount = amount,
                Type = WalletTransactionType.Credit,
                Description = description,
                BalanceAfter = user.WalletBalance,
                OrderId = orderId,
                CreatedAt = DateTime.UtcNow
            };

            await _context.WalletTransactions.AddAsync(transaction);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DebitWalletAsync(string userId, decimal amount, string description, int? orderId = null)
        {
            if (amount <= 0) return false;

            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.WalletBalance < amount) return false;

            user.WalletBalance -= amount;

            var transaction = new WalletTransaction
            {
                UserId = userId,
                Amount = amount,
                Type = WalletTransactionType.Debit,
                Description = description,
                BalanceAfter = user.WalletBalance,
                OrderId = orderId,
                CreatedAt = DateTime.UtcNow
            };

            await _context.WalletTransactions.AddAsync(transaction);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ConvertPointsToWalletAsync(string userId, int points)
        {
            if (points < 10) return false;

            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.LoyaltyPoints < points) return false;

            user.LoyaltyPoints -= points;
            decimal cashAmount = points * 1.0m; // 1 point = ₹1
            user.WalletBalance += cashAmount;

            var transaction = new WalletTransaction
            {
                UserId = userId,
                Amount = cashAmount,
                Type = WalletTransactionType.Credit,
                Description = $"Redeemed {points} Loyalty Reward Points to Store Wallet Cash",
                BalanceAfter = user.WalletBalance,
                CreatedAt = DateTime.UtcNow
            };

            await _context.WalletTransactions.AddAsync(transaction);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task AddLoyaltyPointsAsync(string userId, int points, string description)
        {
            if (points <= 0) return;

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return;

            user.LoyaltyPoints += points;
            await _context.SaveChangesAsync();
        }
    }
}
