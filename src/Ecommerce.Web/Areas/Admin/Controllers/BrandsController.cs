using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;

namespace Ecommerce.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class BrandsController : Controller
    {
        private readonly IBrandService _brandService;

        public BrandsController(IBrandService brandService)
        {
            _brandService = brandService;
        }

        public async Task<IActionResult> Index()
        {
            var brands = await _brandService.GetAllBrandsAsync();
            return View(brands);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BrandVM model)
        {
            if (ModelState.IsValid)
            {
                await _brandService.CreateBrandAsync(model);
                TempData["SuccessMessage"] = "Brand created successfully.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(BrandVM model)
        {
            if (ModelState.IsValid)
            {
                await _brandService.UpdateBrandAsync(model);
                TempData["SuccessMessage"] = "Brand updated successfully.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _brandService.DeleteBrandAsync(id);
            TempData["SuccessMessage"] = "Brand deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
