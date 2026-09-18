using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Enums;

namespace Ecommerce.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class OrdersController : Controller
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        public async Task<IActionResult> Index(OrderStatus? status = null)
        {
            ViewBag.CurrentStatus = status;
            var orders = await _orderService.GetAllAdminOrdersAsync(status);
            return View(orders);
        }

        public async Task<IActionResult> Details(int id)
        {
            var order = await _orderService.GetOrderDetailAsync(id);
            if (order == null) return NotFound();
            return View(order);
        }

        public async Task<IActionResult> Invoice(int id)
        {
            var order = await _orderService.GetOrderDetailAsync(id);
            if (order == null) return NotFound();
            return View("~/Views/Orders/Invoice.cshtml", order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int orderId, OrderStatus status, string? notes)
        {
            await _orderService.UpdateOrderStatusAsync(orderId, status, notes, User.Identity?.Name ?? "Admin");
            TempData["SuccessMessage"] = $"Order status updated to {status}.";
            return RedirectToAction(nameof(Details), new { id = orderId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateShipmentTracking(UpdateShipmentTrackingDto dto)
        {
            try
            {
                await _orderService.UpdateShipmentTrackingAsync(dto, User.Identity?.Name ?? "Admin");
                TempData["SuccessMessage"] = $"Shipment & logistics tracking updated for Order.";
            }
            catch (System.Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id = dto.OrderId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePaymentStatus(int orderId, PaymentStatus paymentStatus, string? notes)
        {
            await _orderService.UpdatePaymentStatusAsync(orderId, paymentStatus, notes, User.Identity?.Name ?? "Admin");
            TempData["SuccessMessage"] = $"Payment status updated to {paymentStatus}.";
            return RedirectToAction(nameof(Details), new { id = orderId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessReturn(ProcessReturnDto dto)
        {
            try
            {
                await _orderService.ProcessReturnAsync(dto, User.Identity?.Name ?? "Admin");
                TempData["SuccessMessage"] = $"Return request processed successfully (Action: {dto.Action}).";
            }
            catch (System.Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id = dto.OrderId });
        }
    }
}
