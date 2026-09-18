using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;

namespace Ecommerce.Web.Controllers
{
    public class CartController : BaseController
    {
        private readonly ICartService _cartService;
        private readonly ICouponService _couponService;

        public CartController(ICartService cartService, ICouponService couponService)
        {
            _cartService = cartService;
            _couponService = couponService;
        }

        public async Task<IActionResult> Index()
        {
            var guestId = GetOrCreateGuestSessionId();
            var cart = await _cartService.GetCartAsync(CurrentUserId, guestId);

            // Check if coupon is stored in session
            var appliedCoupon = HttpContext.Session.GetString("AppliedCouponCode");
            if (!string.IsNullOrEmpty(appliedCoupon) && cart.Items.Count > 0)
            {
                var couponResult = await _couponService.ValidateAndCalculateCouponAsync(appliedCoupon, cart.SubTotal, CurrentUserId);
                if (couponResult.IsValid)
                {
                    cart.AppliedCouponCode = appliedCoupon;
                    cart.DiscountAmount = couponResult.DiscountAmount;
                }
                else
                {
                    HttpContext.Session.Remove("AppliedCouponCode");
                }
            }

            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] AddToCartDto dto)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid product or quantity." });
            }

            try
            {
                var guestId = GetOrCreateGuestSessionId();
                await _cartService.AddToCartAsync(dto, CurrentUserId, guestId);
                var count = await _cartService.GetCartCountAsync(CurrentUserId, guestId);

                return Json(new { success = true, message = "Product added to cart!", cartCount = count });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFromForm(AddToCartDto dto, string? returnUrl)
        {
            var guestId = GetOrCreateGuestSessionId();
            await _cartService.AddToCartAsync(dto, CurrentUserId, guestId);
            TempData["SuccessMessage"] = "Item added to cart successfully!";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity)
        {
            var guestId = GetOrCreateGuestSessionId();
            await _cartService.UpdateQuantityAsync(cartItemId, quantity, CurrentUserId, guestId);

            var cart = await _cartService.GetCartAsync(CurrentUserId, guestId);
            var appliedCoupon = HttpContext.Session.GetString("AppliedCouponCode");
            if (!string.IsNullOrEmpty(appliedCoupon))
            {
                var couponResult = await _couponService.ValidateAndCalculateCouponAsync(appliedCoupon, cart.SubTotal, CurrentUserId);
                if (couponResult.IsValid)
                {
                    cart.AppliedCouponCode = appliedCoupon;
                    cart.DiscountAmount = couponResult.DiscountAmount;
                }
            }

            var item = cart.Items.Find(i => i.Id == cartItemId);
            return Json(new
            {
                success = true,
                itemTotal = item != null ? $"₹{item.TotalPrice:N2}" : "₹0.00",
                subTotal = $"₹{cart.SubTotal:N2}",
                discount = $"₹{cart.DiscountAmount:N2}",
                tax = $"₹{cart.TaxAmount:N2}",
                shipping = cart.ShippingFee == 0 ? "FREE" : $"₹{cart.ShippingFee:N2}",
                grandTotal = $"₹{cart.GrandTotal:N2}",
                cartCount = cart.TotalQuantity
            });
        }

        [HttpPost]
        public async Task<IActionResult> Remove(int cartItemId)
        {
            var guestId = GetOrCreateGuestSessionId();
            await _cartService.RemoveFromCartAsync(cartItemId, CurrentUserId, guestId);
            var count = await _cartService.GetCartCountAsync(CurrentUserId, guestId);

            return Json(new { success = true, cartCount = count, message = "Item removed from cart." });
        }

        [HttpPost]
        public async Task<IActionResult> ApplyCoupon(string code)
        {
            var guestId = GetOrCreateGuestSessionId();
            var cart = await _cartService.GetCartAsync(CurrentUserId, guestId);

            if (cart.Items.Count == 0)
            {
                return Json(new { success = false, message = "Your cart is empty." });
            }

            var (isValid, message, discountAmount) = await _couponService.ValidateAndCalculateCouponAsync(code, cart.SubTotal, CurrentUserId);

            if (isValid)
            {
                HttpContext.Session.SetString("AppliedCouponCode", code.Trim().ToUpper());
                cart.AppliedCouponCode = code.Trim().ToUpper();
                cart.DiscountAmount = discountAmount;

                return Json(new
                {
                    success = true,
                    message,
                    couponCode = code.ToUpper(),
                    discount = $"₹{cart.DiscountAmount:N2}",
                    grandTotal = $"₹{cart.GrandTotal:N2}"
                });
            }

            return Json(new { success = false, message });
        }

        [HttpPost]
        public IActionResult RemoveCoupon()
        {
            HttpContext.Session.Remove("AppliedCouponCode");
            return Json(new { success = true, message = "Coupon removed." });
        }

        [HttpPost]
        public async Task<IActionResult> ClearCart()
        {
            var guestId = GetOrCreateGuestSessionId();
            await _cartService.ClearCartAsync(CurrentUserId, guestId);
            return Json(new { success = true, message = "Cart cleared." });
        }

        [HttpGet]
        public async Task<IActionResult> GetCartCount()
        {
            var guestId = GetOrCreateGuestSessionId();
            var count = await _cartService.GetCartCountAsync(CurrentUserId, guestId);
            return Json(new { count });
        }
    }
}
