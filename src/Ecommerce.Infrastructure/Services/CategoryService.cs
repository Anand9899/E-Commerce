using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Data;

namespace Ecommerce.Infrastructure.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ApplicationDbContext _context;

        public CategoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<CategoryVM>> GetAllCategoriesAsync()
        {
            return await _context.Categories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new CategoryVM
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    Description = c.Description,
                    ImageUrl = c.ImageUrl,
                    IconClass = c.IconClass,
                    ShowOnHome = c.ShowOnHome,
                    ProductCount = c.Products.Count(p => p.IsActive)
                })
                .ToListAsync();
        }

        public async Task<List<CategoryVM>> GetHomeCategoriesAsync()
        {
            return await _context.Categories
                .AsNoTracking()
                .Where(c => c.IsActive && c.ShowOnHome)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new CategoryVM
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    Description = c.Description,
                    ImageUrl = c.ImageUrl,
                    IconClass = c.IconClass,
                    ShowOnHome = c.ShowOnHome,
                    ProductCount = c.Products.Count(p => p.IsActive)
                })
                .ToListAsync();
        }

        public async Task<CategoryVM?> GetCategoryByIdAsync(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return null;
            return new CategoryVM
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug,
                Description = category.Description,
                ImageUrl = category.ImageUrl,
                IconClass = category.IconClass,
                ShowOnHome = category.ShowOnHome
            };
        }

        public async Task<CategoryVM?> GetCategoryBySlugAsync(string slug)
        {
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Slug == slug);
            if (category == null) return null;
            return new CategoryVM
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug,
                Description = category.Description,
                ImageUrl = category.ImageUrl,
                IconClass = category.IconClass,
                ShowOnHome = category.ShowOnHome
            };
        }

        public async Task CreateCategoryAsync(CategoryVM model)
        {
            var slug = model.Slug;
            if (string.IsNullOrWhiteSpace(slug))
            {
                slug = Regex.Replace(model.Name.ToLowerInvariant(), @"[^a-z0-9\s-]", "");
                slug = Regex.Replace(slug, @"\s+", "-").Trim('-');
            }

            var category = new Category
            {
                Name = model.Name.Trim(),
                Slug = slug,
                Description = model.Description,
                ImageUrl = model.ImageUrl,
                IconClass = model.IconClass,
                ShowOnHome = model.ShowOnHome,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Categories.AddAsync(category);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateCategoryAsync(CategoryVM model)
        {
            var category = await _context.Categories.FindAsync(model.Id);
            if (category == null) return;

            category.Name = model.Name.Trim();
            if (!string.IsNullOrWhiteSpace(model.Slug)) category.Slug = model.Slug.Trim();
            category.Description = model.Description;
            category.ImageUrl = model.ImageUrl;
            category.IconClass = model.IconClass;
            category.ShowOnHome = model.ShowOnHome;
            category.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteCategoryAsync(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                category.IsDeleted = true;
                category.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
    }
}
