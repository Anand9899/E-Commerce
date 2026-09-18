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
    public class BrandService : IBrandService
    {
        private readonly ApplicationDbContext _context;

        public BrandService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<BrandVM>> GetAllBrandsAsync()
        {
            return await _context.Brands
                .Where(b => b.IsActive)
                .OrderBy(b => b.Name)
                .Select(b => new BrandVM
                {
                    Id = b.Id,
                    Name = b.Name,
                    Slug = b.Slug,
                    LogoUrl = b.LogoUrl,
                    ProductCount = b.Products.Count(p => p.IsActive)
                })
                .ToListAsync();
        }

        public async Task<BrandVM?> GetBrandByIdAsync(int id)
        {
            var brand = await _context.Brands.FindAsync(id);
            if (brand == null) return null;
            return new BrandVM
            {
                Id = brand.Id,
                Name = brand.Name,
                Slug = brand.Slug,
                LogoUrl = brand.LogoUrl
            };
        }

        public async Task CreateBrandAsync(BrandVM model)
        {
            var slug = model.Slug;
            if (string.IsNullOrWhiteSpace(slug))
            {
                slug = Regex.Replace(model.Name.ToLowerInvariant(), @"[^a-z0-9\s-]", "");
                slug = Regex.Replace(slug, @"\s+", "-").Trim('-');
            }

            var brand = new Brand
            {
                Name = model.Name.Trim(),
                Slug = slug,
                LogoUrl = model.LogoUrl,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Brands.AddAsync(brand);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateBrandAsync(BrandVM model)
        {
            var brand = await _context.Brands.FindAsync(model.Id);
            if (brand == null) return;

            brand.Name = model.Name.Trim();
            if (!string.IsNullOrWhiteSpace(model.Slug)) brand.Slug = model.Slug.Trim();
            brand.LogoUrl = model.LogoUrl;
            brand.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteBrandAsync(int id)
        {
            var brand = await _context.Brands.FindAsync(id);
            if (brand != null)
            {
                brand.IsDeleted = true;
                brand.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
    }
}
