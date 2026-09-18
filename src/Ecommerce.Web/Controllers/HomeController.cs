using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;

namespace Ecommerce.Web.Controllers
{
    /// <summary>
    /// Home Controller:
    /// Website ke Main Landing Page, About Us, Contact Us aur Error handling ko manage karta hai.
    /// </summary>
    public class HomeController : BaseController
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly IContactService _contactService;

        public HomeController(
            IProductService productService, 
            ICategoryService categoryService,
            IContactService contactService)
        {
            _productService = productService;
            _categoryService = categoryService;
            _contactService = contactService;
        }

        /// <summary>
        /// Home Page: Featured Products, Trending Products aur Top Categories load karta hai.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            ViewBag.FeaturedProducts = await _productService.GetFeaturedProductsAsync(8, CurrentUserId);
            ViewBag.TrendingProducts = await _productService.GetTrendingProductsAsync(8, CurrentUserId);
            ViewBag.Categories = await _categoryService.GetHomeCategoriesAsync();
            return View();
        }

        /// <summary>
        /// About Us Page: Store ki company information aur values display karta hai.
        /// </summary>
        public IActionResult About()
        {
            return View();
        }

        /// <summary>
        /// Contact Us Page (GET): Inquiry / Complaint form render karta hai.
        /// </summary>
        public IActionResult Contact()
        {
            return View(new ContactFormVM());
        }

        /// <summary>
        /// Contact Form Submission (POST):
        /// User dwara fill kiya gaya message validate karta hai, database me save karta hai
        /// aur Admin ko background email notification bhejta hai.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(ContactFormVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                await _contactService.SubmitContactMessageAsync(model);
                TempData["SuccessMessage"] = "Thank you! Your inquiry / complaint has been received. Our support team will review and contact you shortly.";
                return RedirectToAction(nameof(Contact));
            }
            catch (System.Exception ex)
            {
                ModelState.AddModelError("", "Unable to send your message right now. Please try again later. " + ex.Message);
                return View(model);
            }
        }

        /// <summary>
        /// Error Page: Global error ya 404/500 issues ke waqt fallback error view dikhata hai.
        /// </summary>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}
