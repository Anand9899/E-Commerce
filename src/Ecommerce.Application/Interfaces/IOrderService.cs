using System.Collections.Generic;
using System.Threading.Tasks;
using Ecommerce.Application.DTOs;
using Ecommerce.Domain.Enums;

namespace Ecommerce.Application.Interfaces
{
    public interface IOrderService
    {
        Task<int> PlaceOrderAsync(CheckoutVM model, string userId, string? guestSessionId = null);
        Task<List<OrderListVM>> GetUserOrdersAsync(string userId);
        Task<OrderDetailVM?> GetOrderDetailAsync(int orderId, string? userId = null);
        Task<OrderDetailVM?> GetOrderByNumberAsync(string orderNumber, string? userId = null);
        Task<List<OrderListVM>> GetAllAdminOrdersAsync(OrderStatus? statusFilter = null);
        Task UpdateOrderStatusAsync(int orderId, OrderStatus newStatus, string? notes, string changedBy);
        Task UpdateShipmentTrackingAsync(UpdateShipmentTrackingDto dto, string changedBy);
        Task UpdatePaymentStatusAsync(int orderId, PaymentStatus newStatus, string? notes, string changedBy);
        Task CancelOrderAsync(int orderId, string userId, string reason);
        Task RequestReturnAsync(ReturnRequestDto dto, string userId);
        Task ProcessReturnAsync(ProcessReturnDto dto, string adminUser);
    }
}
