using System;
using System.Collections.Generic;
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
    public class CouponService : ICouponService
    {
        private readonly ApplicationDbContext _context;

        public CouponService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(bool IsValid, string Message, decimal DiscountAmount)> ValidateAndCalculateCouponAsync(string code, decimal subTotal, string? userId)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return (false, "Please enter a valid coupon code.", 0);
            }

            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.Code.ToUpper() == code.Trim().ToUpper() && c.IsActive);

            if (coupon == null)
            {
                return (false, "Coupon code is invalid or does not exist.", 0);
            }

            if (DateTime.UtcNow < coupon.StartDate || DateTime.UtcNow > coupon.ExpiryDate)
            {
                return (false, "This coupon code has expired.", 0);
            }

            if (coupon.UsageLimit.HasValue && coupon.UsedCount >= coupon.UsageLimit.Value)
            {
                return (false, "This coupon's maximum usage limit has been reached.", 0);
            }

            if (subTotal < coupon.MinOrderAmount)
            {
                return (false, $"Minimum order amount of ₹{coupon.MinOrderAmount:N2} required for this coupon.", 0);
            }

            if (!string.IsNullOrEmpty(userId))
            {
                var userUsageCount = await _context.CouponUsages
                    .CountAsync(cu => cu.CouponId == coupon.Id && cu.UserId == userId);

                if (userUsageCount >= 1) // One use per user default rule
                {
                    return (false, "You have already used this coupon code.", 0);
                }
            }

            decimal discount = 0;
            if (coupon.DiscountType == DiscountType.Percentage)
            {
                discount = Math.Round(subTotal * (coupon.DiscountValue / 100m), 2);
                if (coupon.MaxDiscountAmount.HasValue && discount > coupon.MaxDiscountAmount.Value)
                {
                    discount = coupon.MaxDiscountAmount.Value;
                }
            }
            else
            {
                discount = coupon.DiscountValue;
            }

            if (discount > subTotal) discount = subTotal;

            return (true, $"Coupon applied! You saved ₹{discount:N2}", discount);
        }

        public async Task<List<CouponVM>> GetAllCouponsAsync()
        {
            return await _context.Coupons
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CouponVM
                {
                    Id = c.Id,
                    Code = c.Code,
                    Description = c.Description,
                    DiscountType = c.DiscountType,
                    DiscountValue = c.DiscountValue,
                    MinOrderAmount = c.MinOrderAmount,
                    MaxDiscountAmount = c.MaxDiscountAmount,
                    ExpiryDate = c.ExpiryDate,
                    IsActive = c.IsActive
                })
                .ToListAsync();
        }

        public async Task CreateCouponAsync(CouponVM coupon)
        {
            var entity = new Coupon
            {
                Code = coupon.Code.Trim().ToUpper(),
                Description = coupon.Description,
                DiscountType = coupon.DiscountType,
                DiscountValue = coupon.DiscountValue,
                MinOrderAmount = coupon.MinOrderAmount,
                MaxDiscountAmount = coupon.MaxDiscountAmount,
                StartDate = DateTime.UtcNow,
                ExpiryDate = coupon.ExpiryDate,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Coupons.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task ToggleCouponStatusAsync(int id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon != null)
            {
                coupon.IsActive = !coupon.IsActive;
                coupon.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteCouponAsync(int id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon != null)
            {
                coupon.IsDeleted = true;
                await _context.SaveChangesAsync();
            }
        }
    }
}
