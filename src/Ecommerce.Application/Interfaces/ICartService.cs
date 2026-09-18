using System.Threading.Tasks;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Interfaces
{
    public interface ICartService
    {
        Task<CartVM> GetCartAsync(string? userId, string? guestSessionId);
        Task AddToCartAsync(AddToCartDto dto, string? userId, string? guestSessionId);
        Task UpdateQuantityAsync(int cartItemId, int quantity, string? userId, string? guestSessionId);
        Task RemoveFromCartAsync(int cartItemId, string? userId, string? guestSessionId);
        Task ClearCartAsync(string? userId, string? guestSessionId);
        Task MergeGuestCartToUserAsync(string guestSessionId, string userId);
        Task<int> GetCartCountAsync(string? userId, string? guestSessionId);
    }
}
