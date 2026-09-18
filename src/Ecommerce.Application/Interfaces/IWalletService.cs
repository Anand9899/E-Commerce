using System.Threading.Tasks;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Interfaces
{
    public interface IWalletService
    {
        Task<WalletSummaryVM> GetWalletSummaryAsync(string userId);
        Task<bool> CreditWalletAsync(string userId, decimal amount, string description, int? orderId = null);
        Task<bool> DebitWalletAsync(string userId, decimal amount, string description, int? orderId = null);
        Task<bool> ConvertPointsToWalletAsync(string userId, int points);
        Task AddLoyaltyPointsAsync(string userId, int points, string description);
    }
}
