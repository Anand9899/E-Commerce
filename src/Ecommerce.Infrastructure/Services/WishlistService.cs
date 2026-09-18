using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Data;

namespace Ecommerce.Infrastructure.Services
{
    public class WishlistService : IWishlistService
    {
        private readonly ApplicationDbContext _context;

        public WishlistService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ProductListItemVM>> GetUserWishlistAsync(string userId)
        {
            return await _context.WishlistItems
                .Include(w => w.Product)
                    .ThenInclude(p => p.Category)
                .Include(w => w.Product)
                    .ThenInclude(p => p.Images)
                .Where(w => w.UserId == userId && w.Product.IsActive)
                .OrderByDescending(w => w.CreatedAt)
                .Select(w => new ProductListItemVM
                {
                    Id = w.Product.Id,
                    Name = w.Product.Name,
                    Slug = w.Product.Slug,
                    SKU = w.Product.SKU,
                    BasePrice = w.Product.BasePrice,
                    DiscountPrice = w.Product.DiscountPrice,
                    StockQuantity = w.Product.StockQuantity,
                    CategoryName = w.Product.Category.Name,
                    PrimaryImageUrl = w.Product.Images.FirstOrDefault(i => i.IsPrimary) != null 
                        ? w.Product.Images.FirstOrDefault(i => i.IsPrimary)!.ImageUrl 
                        : (w.Product.Images.FirstOrDefault() != null ? w.Product.Images.FirstOrDefault()!.ImageUrl : "/images/placeholder-product.png"),
                    AverageRating = w.Product.AverageRating,
                    TotalReviews = w.Product.TotalReviews,
                    IsInWishlist = true
                })
                .ToListAsync();
        }

        public async Task<bool> ToggleWishlistAsync(int productId, string userId)
        {
            var item = await _context.WishlistItems
                .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

            if (item != null)
            {
                _context.WishlistItems.Remove(item);
                await _context.SaveChangesAsync();
                return false; // Removed
            }
            else
            {
                var newItem = new WishlistItem
                {
                    UserId = userId,
                    ProductId = productId,
                    CreatedAt = DateTime.UtcNow
                };
                await _context.WishlistItems.AddAsync(newItem);
                await _context.SaveChangesAsync();
                return true; // Added
            }
        }

        public async Task<bool> IsInWishlistAsync(int productId, string userId)
        {
            return await _context.WishlistItems.AnyAsync(w => w.UserId == userId && w.ProductId == productId);
        }

        public async Task<int> GetWishlistCountAsync(string userId)
        {
            return await _context.WishlistItems.CountAsync(w => w.UserId == userId);
        }
    }
}
