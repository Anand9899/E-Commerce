using System.Collections.Generic;
using System.Threading.Tasks;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Interfaces
{
    public interface IWishlistService
    {
        Task<List<ProductListItemVM>> GetUserWishlistAsync(string userId);
        Task<bool> ToggleWishlistAsync(int productId, string userId);
        Task<bool> IsInWishlistAsync(int productId, string userId);
        Task<int> GetWishlistCountAsync(string userId);
    }
}
