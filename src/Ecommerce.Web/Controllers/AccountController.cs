using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Web.Controllers
{
    public class AccountController : BaseController
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ICartService _cartService;
        private readonly IAddressService _addressService;
        private readonly IOrderService _orderService;
        private readonly IWishlistService _wishlistService;
        private readonly IWalletService _walletService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ICartService cartService,
            IAddressService addressService,
            IOrderService orderService,
            IWishlistService wishlistService,
            IWalletService walletService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _cartService = cartService;
            _addressService = addressService;
            _orderService = orderService;
            _wishlistService = wishlistService;
            _walletService = walletService;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View(new LoginVM { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !user.IsActive)
            {
                ModelState.AddModelError("", "Invalid login attempt or account is disabled.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                user.LastLoginAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                // Merge guest cart if any
                var guestId = HttpContext.Session.GetString("GuestSessionId");
                if (!string.IsNullOrEmpty(guestId))
                {
                    await _cartService.MergeGuestCartToUserAsync(guestId, user.Id);
                }

                // Check Admin role
                if (await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    if (string.IsNullOrEmpty(model.ReturnUrl) || model.ReturnUrl == "/")
                    {
                        return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                    }
                }

                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }
                return RedirectToAction("Index", "Shop");
            }

            ModelState.AddModelError("", "Invalid email or password.");
            return View(model);
        }

        [HttpGet]
        public IActionResult ForgotPassword(string? email = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Shop");
            }
            return View(new ForgotPasswordVM { Email = email ?? "" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordVM model)
        {
            if (!string.IsNullOrWhiteSpace(model.Email) && !model.Email.Trim().EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("Email", "Only valid @gmail.com email addresses are allowed.");
            }

            if (!string.IsNullOrWhiteSpace(model.NewPassword) && !System.Text.RegularExpressions.Regex.IsMatch(model.NewPassword, @"^(?=.*[0-9])(?=.*[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?])[A-Z].*$"))
            {
                ModelState.AddModelError("NewPassword", "Password must start with a Capital letter (A-Z) and contain at least one number and one special symbol (e.g. Anand@123).");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email.Trim().ToLower());
            if (user == null)
            {
                ModelState.AddModelError("Email", "No account registered with this @gmail.com address.");
                return View(model);
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
            if (resetResult.Succeeded)
            {
                user.PlainPassword = model.NewPassword;
                await _userManager.UpdateAsync(user);

                TempData["SuccessMessage"] = "Password reset successful! You can now sign in with your new password.";
                return RedirectToAction("Login", "Account");
            }

            foreach (var error in resetResult.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Register(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Shop");
            }
            return View(new RegisterVM { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVM model)
        {
            // 1. Auto-capitalize Name (Title Case: First letter of each word capital)
            if (!string.IsNullOrWhiteSpace(model.FullName))
            {
                var textInfo = System.Globalization.CultureInfo.CurrentCulture.TextInfo;
                model.FullName = textInfo.ToTitleCase(model.FullName.Trim().ToLower());
            }

            // 2. Normalize and validate 10-digit Phone Number
            var digitsOnly = System.Text.RegularExpressions.Regex.Replace(model.PhoneNumber ?? "", @"[^\d]", "");
            if (digitsOnly.Length > 10 && digitsOnly.StartsWith("91"))
            {
                digitsOnly = digitsOnly.Substring(digitsOnly.Length - 10);
            }
            model.PhoneNumber = digitsOnly;

            if (digitsOnly.Length != 10)
            {
                ModelState.AddModelError("PhoneNumber", "Mobile number must be exactly 10 digits (e.g. 9876543210).");
            }

            // 3. Email Authentication (@gmail.com check)
            if (!string.IsNullOrWhiteSpace(model.Email) && !model.Email.Trim().EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("Email", "Only valid @gmail.com email addresses are allowed (e.g. yourname@gmail.com).");
            }

            // 4. Password validation (Starts with Capital, has special symbol, has number)
            if (!string.IsNullOrWhiteSpace(model.Password) && !System.Text.RegularExpressions.Regex.IsMatch(model.Password, @"^(?=.*[0-9])(?=.*[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?])[A-Z].*$"))
            {
                ModelState.AddModelError("Password", "Password must start with a Capital letter (A-Z) and contain at least one number and one special symbol (e.g. Anand@123).");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email.Trim().ToLower(),
                Email = model.Email.Trim().ToLower(),
                FullName = model.FullName,
                PhoneNumber = "+91 " + digitsOnly,
                Gender = model.Gender,
                CreatedAt = DateTime.UtcNow,
                PlainPassword = model.Password,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Customer");

                TempData["SuccessMessage"] = "Registration successful! Please sign in with your email and password to start shopping.";
                
                return RedirectToAction("Login", "Account", new { returnUrl = model.ReturnUrl });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.FindByIdAsync(CurrentUserId!);
            if (user == null) return NotFound();

            var orders = await _orderService.GetUserOrdersAsync(user.Id);
            var wishlistCount = await _wishlistService.GetWishlistCountAsync(user.Id);
            var addresses = await _addressService.GetUserAddressesAsync(user.Id);
            var roles = await _userManager.GetRolesAsync(user);

            var vm = new UserProfileVM
            {
                FullName = user.FullName,
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber ?? "",
                Gender = user.Gender,
                AvatarUrl = user.AvatarUrl,
                PlainPassword = user.PlainPassword,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                TotalOrders = orders.Count,
                WishlistCount = wishlistCount,
                AddressCount = addresses.Count,
                Role = roles.FirstOrDefault() ?? "Customer"
            };

            return View(vm);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(UserProfileVM model)
        {
            var user = await _userManager.FindByIdAsync(CurrentUserId!);
            if (user == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(model.FullName))
            {
                var textInfo = System.Globalization.CultureInfo.CurrentCulture.TextInfo;
                user.FullName = textInfo.ToTitleCase(model.FullName.Trim().ToLower());
            }

            if (!string.IsNullOrWhiteSpace(model.PhoneNumber))
            {
                var digitsOnly = System.Text.RegularExpressions.Regex.Replace(model.PhoneNumber, @"[^\d]", "");
                if (digitsOnly.Length > 10 && digitsOnly.StartsWith("91"))
                {
                    digitsOnly = digitsOnly.Substring(digitsOnly.Length - 10);
                }
                user.PhoneNumber = digitsOnly.Length == 10 ? "+91 " + digitsOnly : model.PhoneNumber.Trim();
            }

            user.Gender = model.Gender;
            user.AvatarUrl = model.AvatarUrl;

            // If password changed
            if (!string.IsNullOrWhiteSpace(model.PlainPassword) && model.PlainPassword != user.PlainPassword)
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(model.PlainPassword, @"^(?=.*[0-9])(?=.*[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?])[A-Z].*$"))
                {
                    TempData["ErrorMessage"] = "Password must start with a Capital letter (A-Z) and contain at least one number and one special symbol (e.g. Anand@123).";
                    return RedirectToAction(nameof(Profile));
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetResult = await _userManager.ResetPasswordAsync(user, token, model.PlainPassword);
                if (resetResult.Succeeded)
                {
                    user.PlainPassword = model.PlainPassword;
                }
                else
                {
                    TempData["ErrorMessage"] = string.Join(", ", resetResult.Errors.Select(e => e.Description));
                    return RedirectToAction(nameof(Profile));
                }
            }

            await _userManager.UpdateAsync(user);
            TempData["SuccessMessage"] = "Profile details updated successfully!";
            return RedirectToAction(nameof(Profile));
        }

        [Authorize]
        public async Task<IActionResult> Addresses()
        {
            var addresses = await _addressService.GetUserAddressesAsync(CurrentUserId!);
            return View(addresses);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAddress(AddressVM model)
        {
            if (ModelState.IsValid)
            {
                await _addressService.SaveAddressAsync(model, CurrentUserId!);
                TempData["SuccessMessage"] = "Address saved successfully.";
            }
            return RedirectToAction(nameof(Addresses));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetDefaultAddress(int id)
        {
            await _addressService.SetDefaultAddressAsync(id, CurrentUserId!);
            TempData["SuccessMessage"] = "Default address updated.";
            return RedirectToAction(nameof(Addresses));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            await _addressService.DeleteAddressAsync(id, CurrentUserId!);
            TempData["SuccessMessage"] = "Address deleted.";
            return RedirectToAction(nameof(Addresses));
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Wallet()
        {
            var summary = await _walletService.GetWalletSummaryAsync(CurrentUserId!);
            return View(summary);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TopUpWallet(TopUpWalletDto dto)
        {
            if (ModelState.IsValid)
            {
                await _walletService.CreditWalletAsync(CurrentUserId!, dto.Amount, $"Wallet Top-Up via {dto.PaymentMethod}");
                TempData["SuccessMessage"] = $"₹{dto.Amount:N2} added to your Cartivo Wallet!";
            }
            return RedirectToAction(nameof(Wallet));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RedeemLoyaltyPoints(RedeemPointsDto dto)
        {
            if (ModelState.IsValid)
            {
                var success = await _walletService.ConvertPointsToWalletAsync(CurrentUserId!, dto.Points);
                if (success)
                {
                    TempData["SuccessMessage"] = $"Successfully converted {dto.Points} Loyalty Points to ₹{dto.Points:N2} wallet cash!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Insufficient loyalty points or minimum 10 points required.";
                }
            }
            return RedirectToAction(nameof(Wallet));
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}

