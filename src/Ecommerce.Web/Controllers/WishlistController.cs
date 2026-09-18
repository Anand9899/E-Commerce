using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ecommerce.Application.Interfaces;

namespace Ecommerce.Web.Controllers
{
    [Authorize]
    public class WishlistController : BaseController
    {
        private readonly IWishlistService _wishlistService;

        public WishlistController(IWishlistService wishlistService)
        {
            _wishlistService = wishlistService;
        }

        public async Task<IActionResult> Index()
        {
            var wishlist = await _wishlistService.GetUserWishlistAsync(CurrentUserId!);
            return View(wishlist);
        }

        [HttpPost]
        public async Task<IActionResult> Toggle(int productId)
        {
            if (string.IsNullOrEmpty(CurrentUserId))
            {
                return Json(new { success = false, redirect = Url.Action("Login", "Account") });
            }

            var added = await _wishlistService.ToggleWishlistAsync(productId, CurrentUserId);
            var count = await _wishlistService.GetWishlistCountAsync(CurrentUserId);

            return Json(new
            {
                success = true,
                added,
                message = added ? "Added to your wishlist!" : "Removed from your wishlist.",
                count
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetCount()
        {
            if (string.IsNullOrEmpty(CurrentUserId))
            {
                return Json(new { count = 0 });
            }
            var count = await _wishlistService.GetWishlistCountAsync(CurrentUserId);
            return Json(new { count });
        }
    }
}
