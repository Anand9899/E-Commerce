using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;

namespace Ecommerce.Web.Controllers
{
    [Authorize]
    public class OrdersController : BaseController
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        public async Task<IActionResult> Index()
        {
            var orders = await _orderService.GetUserOrdersAsync(CurrentUserId!);
            return View(orders);
        }

        public async Task<IActionResult> Details(int id)
        {
            var order = await _orderService.GetOrderDetailAsync(id, CurrentUserId);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        public async Task<IActionResult> Invoice(int id)
        {
            var order = await _orderService.GetOrderDetailAsync(id, CurrentUserId);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        public async Task<IActionResult> Track(string orderNumber)
        {
            if (string.IsNullOrWhiteSpace(orderNumber))
            {
                return RedirectToAction(nameof(Index));
            }

            var order = await _orderService.GetOrderByNumberAsync(orderNumber.Trim(), CurrentUserId);
            if (order == null)
            {
                TempData["ErrorMessage"] = $"No order found matching '{orderNumber}'.";
                return RedirectToAction(nameof(Index));
            }

            return View("Details", order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string reason)
        {
            try
            {
                await _orderService.CancelOrderAsync(id, CurrentUserId!, reason);
                TempData["SuccessMessage"] = "Your order has been cancelled successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestReturn(ReturnRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please select a valid return reason and provide comments.";
                return RedirectToAction(nameof(Details), new { id = dto.OrderId });
            }

            try
            {
                await _orderService.RequestReturnAsync(dto, CurrentUserId!);
                TempData["SuccessMessage"] = "Your return request has been submitted successfully. Our team will review and process your request.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id = dto.OrderId });
        }
    }
}
