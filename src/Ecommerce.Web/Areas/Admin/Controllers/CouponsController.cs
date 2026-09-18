using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;

namespace Ecommerce.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CouponsController : Controller
    {
        private readonly ICouponService _couponService;

        public CouponsController(ICouponService couponService)
        {
            _couponService = couponService;
        }

        public async Task<IActionResult> Index()
        {
            var coupons = await _couponService.GetAllCouponsAsync();
            return View(coupons);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CouponVM model)
        {
            if (ModelState.IsValid)
            {
                await _couponService.CreateCouponAsync(model);
                TempData["SuccessMessage"] = "Coupon created successfully.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            await _couponService.ToggleCouponStatusAsync(id);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _couponService.DeleteCouponAsync(id);
            TempData["SuccessMessage"] = "Coupon deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
