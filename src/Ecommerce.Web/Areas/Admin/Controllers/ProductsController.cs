using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;

namespace Ecommerce.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductsController : Controller
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly IBrandService _brandService;

        public ProductsController(
            IProductService productService, 
            ICategoryService categoryService, 
            IBrandService brandService)
        {
            _productService = productService;
            _categoryService = categoryService;
            _brandService = brandService;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _productService.GetAllAdminProductsAsync();
            return View(products);
        }

        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            return View(new CreateEditProductVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateEditProductVM model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdowns();
                return View(model);
            }

            try
            {
                await _productService.CreateProductAsync(model);
                TempData["SuccessMessage"] = "Product created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                await PopulateDropdowns();
                return View(model);
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            var model = await _productService.GetProductForEditAsync(id);
            if (model == null) return NotFound();

            await PopulateDropdowns(model.CategoryId, model.BrandId);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CreateEditProductVM model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(model.CategoryId, model.BrandId);
                return View(model);
            }

            try
            {
                await _productService.UpdateProductAsync(model);
                TempData["SuccessMessage"] = "Product updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                await PopulateDropdowns(model.CategoryId, model.BrandId);
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _productService.DeleteProductAsync(id);
            TempData["SuccessMessage"] = "Product deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdowns(int? selectedCat = null, int? selectedBrand = null)
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            var brands = await _brandService.GetAllBrandsAsync();

            ViewBag.Categories = new SelectList(categories, "Id", "Name", selectedCat);
            ViewBag.Brands = new SelectList(brands, "Id", "Name", selectedBrand);
        }
    }
}
