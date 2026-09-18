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
    public class CartService : ICartService
    {
        private readonly ApplicationDbContext _context;

        public CartService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CartVM> GetCartAsync(string? userId, string? guestSessionId)
        {
            var query = _context.CartItems
                .Include(c => c.Product)
                    .ThenInclude(p => p.Images)
                .Include(c => c.ProductVariant)
                .AsQueryable();

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(c => c.UserId == userId);
            }
            else if (!string.IsNullOrEmpty(guestSessionId))
            {
                query = query.Where(c => c.GuestSessionId == guestSessionId);
            }
            else
            {
                return new CartVM();
            }

            var items = await query.ToListAsync();

            var vm = new CartVM
            {
                Items = items.Select(i => new CartItemVM
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.Product.Name,
                    ProductSlug = i.Product.Slug,
                    ImageUrl = i.Product.Images.FirstOrDefault(img => img.IsPrimary)?.ImageUrl 
                               ?? i.Product.Images.FirstOrDefault()?.ImageUrl 
                               ?? "/images/placeholder-product.png",
                    ProductVariantId = i.ProductVariantId,
                    VariantName = i.ProductVariant?.VariantName,
                    UnitPrice = i.UnitPrice,
                    Quantity = i.Quantity,
                    AvailableStock = i.ProductVariant?.StockQuantity ?? i.Product.StockQuantity
                }).ToList()
            };

            return vm;
        }

        public async Task AddToCartAsync(AddToCartDto dto, string? userId, string? guestSessionId)
        {
            var product = await _context.Products.FindAsync(dto.ProductId);
            if (product == null || !product.IsActive) return;

            decimal unitPrice = product.DiscountPrice.HasValue && product.DiscountPrice.Value > 0 
                ? product.DiscountPrice.Value 
                : product.BasePrice;

            if (dto.ProductVariantId.HasValue && dto.ProductVariantId.Value > 0)
            {
                var variant = await _context.ProductVariants.FindAsync(dto.ProductVariantId.Value);
                if (variant != null)
                {
                    unitPrice = variant.Price;
                }
            }

            CartItem? existingItem = null;
            if (!string.IsNullOrEmpty(userId))
            {
                existingItem = await _context.CartItems.FirstOrDefaultAsync(c => 
                    c.UserId == userId && 
                    c.ProductId == dto.ProductId && 
                    c.ProductVariantId == dto.ProductVariantId);
            }
            else if (!string.IsNullOrEmpty(guestSessionId))
            {
                existingItem = await _context.CartItems.FirstOrDefaultAsync(c => 
                    c.GuestSessionId == guestSessionId && 
                    c.ProductId == dto.ProductId && 
                    c.ProductVariantId == dto.ProductVariantId);
            }

            if (existingItem != null)
            {
                existingItem.Quantity += dto.Quantity;
                existingItem.UnitPrice = unitPrice;
                existingItem.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                var newItem = new CartItem
                {
                    UserId = userId,
                    GuestSessionId = guestSessionId,
                    ProductId = dto.ProductId,
                    ProductVariantId = dto.ProductVariantId > 0 ? dto.ProductVariantId : null,
                    Quantity = dto.Quantity,
                    UnitPrice = unitPrice,
                    CreatedAt = DateTime.UtcNow
                };
                await _context.CartItems.AddAsync(newItem);
            }

            await _context.SaveChangesAsync();
        }

        public async Task UpdateQuantityAsync(int cartItemId, int quantity, string? userId, string? guestSessionId)
        {
            var item = await _context.CartItems.FindAsync(cartItemId);
            if (item == null) return;

            if (quantity <= 0)
            {
                _context.CartItems.Remove(item);
            }
            else
            {
                item.Quantity = quantity;
                item.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task RemoveFromCartAsync(int cartItemId, string? userId, string? guestSessionId)
        {
            var item = await _context.CartItems.FindAsync(cartItemId);
            if (item != null)
            {
                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
            }
        }

        public async Task ClearCartAsync(string? userId, string? guestSessionId)
        {
            var query = _context.CartItems.AsQueryable();
            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(c => c.UserId == userId);
            }
            else if (!string.IsNullOrEmpty(guestSessionId))
            {
                query = query.Where(c => c.GuestSessionId == guestSessionId);
            }
            else return;

            _context.CartItems.RemoveRange(query);
            await _context.SaveChangesAsync();
        }

        public async Task MergeGuestCartToUserAsync(string guestSessionId, string userId)
        {
            if (string.IsNullOrEmpty(guestSessionId) || string.IsNullOrEmpty(userId)) return;

            var guestItems = await _context.CartItems
                .Where(c => c.GuestSessionId == guestSessionId)
                .ToListAsync();

            foreach (var guestItem in guestItems)
            {
                var userItem = await _context.CartItems.FirstOrDefaultAsync(c =>
                    c.UserId == userId &&
                    c.ProductId == guestItem.ProductId &&
                    c.ProductVariantId == guestItem.ProductVariantId);

                if (userItem != null)
                {
                    userItem.Quantity += guestItem.Quantity;
                    _context.CartItems.Remove(guestItem);
                }
                else
                {
                    guestItem.UserId = userId;
                    guestItem.GuestSessionId = null;
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<int> GetCartCountAsync(string? userId, string? guestSessionId)
        {
            if (!string.IsNullOrEmpty(userId))
            {
                return await _context.CartItems.Where(c => c.UserId == userId).SumAsync(c => c.Quantity);
            }
            if (!string.IsNullOrEmpty(guestSessionId))
            {
                return await _context.CartItems.Where(c => c.GuestSessionId == guestSessionId).SumAsync(c => c.Quantity);
            }
            return 0;
        }
    }
}
