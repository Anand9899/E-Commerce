using System.Collections.Generic;
using System.Threading.Tasks;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Interfaces
{
    public interface ICouponService
    {
        Task<(bool IsValid, string Message, decimal DiscountAmount)> ValidateAndCalculateCouponAsync(string code, decimal subTotal, string? userId);
        Task<List<CouponVM>> GetAllCouponsAsync();
        Task CreateCouponAsync(CouponVM coupon);
        Task ToggleCouponStatusAsync(int id);
        Task DeleteCouponAsync(int id);
    }
}
