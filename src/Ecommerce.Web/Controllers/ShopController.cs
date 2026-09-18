using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;

namespace Ecommerce.Web.Controllers
{
    public class ShopController : BaseController
    {
        private readonly IProductService _productService;
        private readonly IReviewService _reviewService;

        public ShopController(IProductService productService, IReviewService reviewService)
        {
            _productService = productService;
            _reviewService = reviewService;
        }

        public async Task<IActionResult> Index(ProductFilterVM filter)
        {
            var model = await _productService.GetFilteredProductsAsync(filter, CurrentUserId);
            return View(model);
        }

        public async Task<IActionResult> Category(string slug, ProductFilterVM filter)
        {
            filter.CategorySlug = slug;
            var model = await _productService.GetFilteredProductsAsync(filter, CurrentUserId);
            return View("Index", model);
        }

        [HttpGet]
        public async Task<IActionResult> QuickSearch(string? q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
            {
                return Json(new { success = true, results = new List<ProductQuickSearchResultDto>(), total = 0 });
            }

            var results = await _productService.QuickSearchAsync(q.Trim(), 6);
            return Json(new 
            { 
                success = true, 
                results, 
                total = results.Count,
                viewAllUrl = Url.Action("Index", "Shop", new { SearchTerm = q.Trim() })
            });
        }

        public async Task<IActionResult> Details(string slug)
        {
            var product = await _productService.GetProductDetailBySlugAsync(slug, CurrentUserId);
            if (product == null)
            {
                return NotFound();
            }

            ViewBag.CanReview = User.Identity?.IsAuthenticated == true && 
                                await _reviewService.CanUserReviewProductAsync(product.Id, CurrentUserId!);

            return View(product);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(CreateReviewDto dto, string returnSlug)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please fill in all review fields with valid rating.";
                return RedirectToAction(nameof(Details), new { slug = returnSlug });
            }

            try
            {
                await _reviewService.AddReviewAsync(dto, CurrentUserId!);
                TempData["SuccessMessage"] = "Your review has been submitted successfully. Thank you!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Details), new { slug = returnSlug });
        }
    }
}
