using System.Collections.Generic;
using System.Threading.Tasks;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Interfaces
{
    public interface IReviewService
    {
        Task<List<ReviewVM>> GetProductReviewsAsync(int productId);
        Task AddReviewAsync(CreateReviewDto dto, string userId);
        Task<bool> CanUserReviewProductAsync(int productId, string userId);
        Task<List<ReviewVM>> GetAllReviewsForAdminAsync();
        Task ToggleReviewApprovalAsync(int reviewId);
        Task DeleteReviewAsync(int reviewId);
    }
}
