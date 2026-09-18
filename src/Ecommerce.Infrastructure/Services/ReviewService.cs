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
    public class ReviewService : IReviewService
    {
        private readonly ApplicationDbContext _context;

        public ReviewService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ReviewVM>> GetProductReviewsAsync(int productId)
        {
            return await _context.Reviews
                .Include(r => r.User)
                .Where(r => r.ProductId == productId && r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ReviewVM
                {
                    Id = r.Id,
                    ProductId = r.ProductId,
                    UserName = r.User.FullName,
                    UserAvatar = r.User.AvatarUrl,
                    Rating = r.Rating,
                    Title = r.Title,
                    Comment = r.Comment,
                    IsVerifiedPurchase = r.IsVerifiedPurchase,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();
        }

        public async Task AddReviewAsync(CreateReviewDto dto, string userId)
        {
            bool isVerified = await _context.Orders
                .AnyAsync(o => o.UserId == userId && 
                               o.Status == OrderStatus.Delivered && 
                               o.OrderItems.Any(oi => oi.ProductId == dto.ProductId));

            var review = new Review
            {
                ProductId = dto.ProductId,
                UserId = userId,
                Rating = dto.Rating,
                Title = dto.Title.Trim(),
                Comment = dto.Comment.Trim(),
                IsVerifiedPurchase = isVerified,
                IsApproved = true,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Reviews.AddAsync(review);
            await _context.SaveChangesAsync();

            // Recalculate average rating for product
            var product = await _context.Products.FindAsync(dto.ProductId);
            if (product != null)
            {
                var approvedReviews = await _context.Reviews
                    .Where(r => r.ProductId == dto.ProductId && r.IsApproved)
                    .ToListAsync();

                product.TotalReviews = approvedReviews.Count;
                product.AverageRating = approvedReviews.Count > 0 ? Math.Round(approvedReviews.Average(r => r.Rating), 1) : 0;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> CanUserReviewProductAsync(int productId, string userId)
        {
            return !await _context.Reviews.AnyAsync(r => r.ProductId == productId && r.UserId == userId);
        }

        public async Task<List<ReviewVM>> GetAllReviewsForAdminAsync()
        {
            return await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Product)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ReviewVM
                {
                    Id = r.Id,
                    ProductId = r.ProductId,
                    UserName = $"{r.User.FullName} (on '{r.Product.Name}')",
                    Rating = r.Rating,
                    Title = r.Title,
                    Comment = r.Comment,
                    IsVerifiedPurchase = r.IsVerifiedPurchase,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();
        }

        public async Task ToggleReviewApprovalAsync(int reviewId)
        {
            var review = await _context.Reviews.FindAsync(reviewId);
            if (review != null)
            {
                review.IsApproved = !review.IsApproved;
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteReviewAsync(int reviewId)
        {
            var review = await _context.Reviews.FindAsync(reviewId);
            if (review != null)
            {
                review.IsDeleted = true;
                await _context.SaveChangesAsync();
            }
        }
    }
}
