using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Web.Controllers
{
    [Authorize]
    public class CheckoutController : BaseController
    {
        private readonly ICartService _cartService;
        private readonly IOrderService _orderService;
        private readonly IAddressService _addressService;
        private readonly ICouponService _couponService;
        private readonly IProductService _productService;
        private readonly UserManager<ApplicationUser> _userManager;

        public CheckoutController(
            ICartService cartService, 
            IOrderService orderService, 
            IAddressService addressService,
            ICouponService couponService,
            IProductService productService,
            UserManager<ApplicationUser> userManager)
        {
            _cartService = cartService;
            _orderService = orderService;
            _addressService = addressService;
            _couponService = couponService;
            _productService = productService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(int? productId = null, int? variantId = null, int quantity = 1)
        {
            CartVM cart;

            if (productId.HasValue && productId.Value > 0)
            {
                // Direct Buy Now for a single specific product
                var product = await _productService.GetProductDetailByIdAsync(productId.Value, CurrentUserId);
                if (product == null)
                {
                    TempData["ErrorMessage"] = "Product not found or is unavailable.";
                    return RedirectToAction("Index", "Shop");
                }

                int buyQty = Math.Max(1, quantity);
                decimal unitPrice = product.EffectivePrice;
                string? variantName = null;

                if (variantId.HasValue && variantId.Value > 0)
                {
                    var variant = product.Variants.FirstOrDefault(v => v.Id == variantId.Value);
                    if (variant != null)
                    {
                        unitPrice = variant.Price;
                        variantName = variant.VariantName;
                    }
                }

                string? primaryImg = product.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl ?? product.Images.FirstOrDefault()?.ImageUrl;

                cart = new CartVM
                {
                    Items = new List<CartItemVM>
                    {
                        new CartItemVM
                        {
                            ProductId = product.Id,
                            ProductName = product.Name,
                            ProductSlug = product.Slug,
                            ImageUrl = primaryImg,
                            UnitPrice = unitPrice,
                            Quantity = buyQty,
                            ProductVariantId = variantId,
                            VariantName = variantName,
                            AvailableStock = product.StockQuantity
                        }
                    }
                };
            }
            else
            {
                cart = await _cartService.GetCartAsync(CurrentUserId, null);
                if (cart.Items.Count == 0)
                {
                    TempData["ErrorMessage"] = "Your cart is empty. Please add products before checking out.";
                    return RedirectToAction("Index", "Cart");
                }

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
            }

            var addresses = await _addressService.GetUserAddressesAsync(CurrentUserId!);
            var defaultAddr = addresses.FirstOrDefault(a => a.IsDefault) ?? addresses.FirstOrDefault();

            var currentUser = await _userManager.FindByIdAsync(CurrentUserId!);
            string resolvedName = !string.IsNullOrWhiteSpace(defaultAddr?.FullName) 
                ? defaultAddr.FullName 
                : (!string.IsNullOrWhiteSpace(currentUser?.FullName) ? currentUser.FullName : "");

            var vm = new CheckoutVM
            {
                Cart = cart,
                SavedAddresses = addresses,
                SelectedAddressId = defaultAddr?.Id,
                ShippingFullName = resolvedName,
                ShippingPhone = defaultAddr?.PhoneNumber ?? "",
                ShippingAddressLine1 = defaultAddr?.AddressLine1 ?? "",
                ShippingAddressLine2 = defaultAddr?.AddressLine2,
                ShippingCity = defaultAddr?.City ?? "",
                ShippingState = defaultAddr?.State ?? "",
                ShippingPostalCode = defaultAddr?.PostalCode ?? "",
                ShippingCountry = defaultAddr?.Country ?? "India",
                DirectProductId = productId,
                DirectVariantId = variantId,
                DirectQuantity = quantity,
                AvailableWalletBalance = currentUser?.WalletBalance ?? 0,
                AvailableLoyaltyPoints = currentUser?.LoyaltyPoints ?? 0
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CheckoutVM model)
        {
            if (model.SelectedAddressId.HasValue && model.SelectedAddressId.Value > 0)
            {
                var saved = await _addressService.GetAddressByIdAsync(model.SelectedAddressId.Value, CurrentUserId!);
                if (saved != null)
                {
                    model.ShippingFullName = saved.FullName;
                    model.ShippingPhone = saved.PhoneNumber;
                    model.ShippingAddressLine1 = saved.AddressLine1;
                    model.ShippingAddressLine2 = saved.AddressLine2;
                    model.ShippingCity = saved.City;
                    model.ShippingState = saved.State;
                    model.ShippingPostalCode = saved.PostalCode;
                    model.ShippingCountry = saved.Country;
                }
            }

            if (string.IsNullOrWhiteSpace(model.ShippingFullName) || 
                string.IsNullOrWhiteSpace(model.ShippingPhone) || 
                string.IsNullOrWhiteSpace(model.ShippingAddressLine1) || 
                string.IsNullOrWhiteSpace(model.ShippingCity) || 
                string.IsNullOrWhiteSpace(model.ShippingPostalCode))
            {
                ModelState.AddModelError("", "Please fill in all required shipping address details.");
                await PopulateCartModelAsync(model);
                return View(nameof(Index), model);
            }

            try
            {
                int orderId = await _orderService.PlaceOrderAsync(model, CurrentUserId!);
                if (!model.DirectProductId.HasValue)
                {
                    HttpContext.Session.Remove("AppliedCouponCode");
                }
                return RedirectToAction(nameof(Confirmation), new { id = orderId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                await PopulateCartModelAsync(model);
                return View(nameof(Index), model);
            }
        }

        public async Task<IActionResult> Confirmation(int id)
        {
            var order = await _orderService.GetOrderDetailAsync(id, CurrentUserId);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        private async Task PopulateCartModelAsync(CheckoutVM model)
        {
            if (model.DirectProductId.HasValue && model.DirectProductId.Value > 0)
            {
                var product = await _productService.GetProductDetailByIdAsync(model.DirectProductId.Value, CurrentUserId);
                if (product != null)
                {
                    int buyQty = Math.Max(1, model.DirectQuantity ?? 1);
                    decimal unitPrice = product.EffectivePrice;
                    string? variantName = null;
                    if (model.DirectVariantId.HasValue && model.DirectVariantId.Value > 0)
                    {
                        var v = product.Variants.FirstOrDefault(x => x.Id == model.DirectVariantId.Value);
                        if (v != null) { unitPrice = v.Price; variantName = v.VariantName; }
                    }
                    string? primaryImg = product.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl ?? product.Images.FirstOrDefault()?.ImageUrl;
                    model.Cart = new CartVM
                    {
                        Items = new List<CartItemVM>
                        {
                            new CartItemVM
                            {
                                ProductId = product.Id,
                                ProductName = product.Name,
                                ProductSlug = product.Slug,
                                ImageUrl = primaryImg,
                                UnitPrice = unitPrice,
                                Quantity = buyQty,
                                ProductVariantId = model.DirectVariantId,
                                VariantName = variantName,
                                AvailableStock = product.StockQuantity
                            }
                        }
                    };
                }
            }
            else
            {
                model.Cart = await _cartService.GetCartAsync(CurrentUserId, null);
            }
            model.SavedAddresses = await _addressService.GetUserAddressesAsync(CurrentUserId!);
        }
    }
}
