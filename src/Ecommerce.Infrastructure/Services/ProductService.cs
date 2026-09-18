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
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork _unitOfWork;

        public ProductService(ApplicationDbContext context, IUnitOfWork unitOfWork)
        {
            _context = context;
            _unitOfWork = unitOfWork;
        }

        public async Task<ProductFilterVM> GetFilteredProductsAsync(ProductFilterVM filter, string? currentUserId = null)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Images)
                .Where(p => p.IsActive)
                .AsNoTracking();

            // Search Filter
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim().ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(term) ||
                                         p.SKU.ToLower().Contains(term) ||
                                         (p.ShortDescription != null && p.ShortDescription.ToLower().Contains(term)) ||
                                         p.Category.Name.ToLower().Contains(term) ||
                                         (p.Brand != null && p.Brand.Name.ToLower().Contains(term)));
            }

            // Category Filter
            if (filter.CategoryId.HasValue && filter.CategoryId.Value > 0)
            {
                query = query.Where(p => p.CategoryId == filter.CategoryId.Value);
            }
            else if (!string.IsNullOrWhiteSpace(filter.CategorySlug))
            {
                query = query.Where(p => p.Category.Slug == filter.CategorySlug);
            }

            // Brand Filter
            if (filter.BrandId.HasValue && filter.BrandId.Value > 0)
            {
                query = query.Where(p => p.BrandId == filter.BrandId.Value);
            }

            // Price Filter
            if (filter.MinPrice.HasValue)
            {
                query = query.Where(p => (p.DiscountPrice ?? p.BasePrice) >= filter.MinPrice.Value);
            }
            if (filter.MaxPrice.HasValue)
            {
                query = query.Where(p => (p.DiscountPrice ?? p.BasePrice) <= filter.MaxPrice.Value);
            }

            // Rating Filter
            if (filter.MinRating.HasValue && filter.MinRating.Value > 0)
            {
                query = query.Where(p => p.AverageRating >= filter.MinRating.Value);
            }

            // In Stock Only
            if (filter.InStockOnly)
            {
                query = query.Where(p => p.StockQuantity > 0);
            }

            // Sorting
            query = filter.SortBy switch
            {
                "price_asc" => query.OrderBy(p => p.DiscountPrice ?? p.BasePrice),
                "price_desc" => query.OrderByDescending(p => p.DiscountPrice ?? p.BasePrice),
                "rating" => query.OrderByDescending(p => p.AverageRating),
                "popular" => query.OrderByDescending(p => p.TotalReviews),
                _ => query.OrderByDescending(p => p.CreatedAt) // "newest"
            };

            filter.TotalItems = await query.CountAsync();

            var userWishlistIds = new HashSet<int>();
            if (!string.IsNullOrEmpty(currentUserId))
            {
                userWishlistIds = (await _context.WishlistItems
                    .Where(w => w.UserId == currentUserId)
                    .Select(w => w.ProductId)
                    .ToListAsync()).ToHashSet();
            }

            var pagedProducts = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(p => new ProductListItemVM
                {
                    Id = p.Id,
                    Name = p.Name,
                    Slug = p.Slug,
                    SKU = p.SKU,
                    ShortDescription = p.ShortDescription,
                    BasePrice = p.BasePrice,
                    DiscountPrice = p.DiscountPrice,
                    StockQuantity = p.StockQuantity,
                    CategoryName = p.Category.Name,
                    CategorySlug = p.Category.Slug,
                    BrandName = p.Brand != null ? p.Brand.Name : null,
                    PrimaryImageUrl = p.Images.Where(i => i.IsPrimary).Select(i => i.ImageUrl).FirstOrDefault() 
                                      ?? p.Images.Select(i => i.ImageUrl).FirstOrDefault() 
                                      ?? "/images/placeholder-product.png",
                    AverageRating = p.AverageRating,
                    TotalReviews = p.TotalReviews,
                    IsFeatured = p.IsFeatured,
                    IsInWishlist = userWishlistIds.Contains(p.Id)
                })
                .ToListAsync();

            filter.Products = pagedProducts;

            // Load Categories with counts for sidebar
            filter.Categories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new CategoryVM
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    ProductCount = c.Products.Count(p => p.IsActive)
                })
                .ToListAsync();

            // Load Brands with counts for sidebar
            filter.Brands = await _context.Brands
                .Where(b => b.IsActive)
                .OrderBy(b => b.Name)
                .Select(b => new BrandVM
                {
                    Id = b.Id,
                    Name = b.Name,
                    Slug = b.Slug,
                    ProductCount = b.Products.Count(p => p.IsActive)
                })
                .ToListAsync();

            return filter;
        }

        public async Task<List<ProductListItemVM>> GetFeaturedProductsAsync(int count = 8, string? currentUserId = null)
        {
            var userWishlistIds = new HashSet<int>();
            if (!string.IsNullOrEmpty(currentUserId))
            {
                userWishlistIds = (await _context.WishlistItems
                    .Where(w => w.UserId == currentUserId)
                    .Select(w => w.ProductId)
                    .ToListAsync()).ToHashSet();
            }

            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Images)
                .Where(p => p.IsActive && p.IsFeatured)
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .Select(p => new ProductListItemVM
                {
                    Id = p.Id,
                    Name = p.Name,
                    Slug = p.Slug,
                    SKU = p.SKU,
                    ShortDescription = p.ShortDescription,
                    BasePrice = p.BasePrice,
                    DiscountPrice = p.DiscountPrice,
                    StockQuantity = p.StockQuantity,
                    CategoryName = p.Category.Name,
                    CategorySlug = p.Category.Slug,
                    BrandName = p.Brand != null ? p.Brand.Name : null,
                    PrimaryImageUrl = p.Images.Where(i => i.IsPrimary).Select(i => i.ImageUrl).FirstOrDefault() 
                                      ?? p.Images.Select(i => i.ImageUrl).FirstOrDefault() 
                                      ?? "/images/placeholder-product.png",
                    AverageRating = p.AverageRating,
                    TotalReviews = p.TotalReviews,
                    IsFeatured = p.IsFeatured,
                    IsInWishlist = userWishlistIds.Contains(p.Id)
                })
                .ToListAsync();
        }

        public async Task<List<ProductListItemVM>> GetTrendingProductsAsync(int count = 8, string? currentUserId = null)
        {
            var userWishlistIds = new HashSet<int>();
            if (!string.IsNullOrEmpty(currentUserId))
            {
                userWishlistIds = (await _context.WishlistItems
                    .Where(w => w.UserId == currentUserId)
                    .Select(w => w.ProductId)
                    .ToListAsync()).ToHashSet();
            }

            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Images)
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.AverageRating)
                .ThenByDescending(p => p.TotalReviews)
                .Take(count)
                .Select(p => new ProductListItemVM
                {
                    Id = p.Id,
                    Name = p.Name,
                    Slug = p.Slug,
                    SKU = p.SKU,
                    ShortDescription = p.ShortDescription,
                    BasePrice = p.BasePrice,
                    DiscountPrice = p.DiscountPrice,
                    StockQuantity = p.StockQuantity,
                    CategoryName = p.Category.Name,
                    CategorySlug = p.Category.Slug,
                    BrandName = p.Brand != null ? p.Brand.Name : null,
                    PrimaryImageUrl = p.Images.Where(i => i.IsPrimary).Select(i => i.ImageUrl).FirstOrDefault() 
                                      ?? p.Images.Select(i => i.ImageUrl).FirstOrDefault() 
                                      ?? "/images/placeholder-product.png",
                    AverageRating = p.AverageRating,
                    TotalReviews = p.TotalReviews,
                    IsFeatured = p.IsFeatured,
                    IsInWishlist = userWishlistIds.Contains(p.Id)
                })
                .ToListAsync();
        }

        public async Task<ProductDetailVM?> GetProductDetailBySlugAsync(string slug, string? currentUserId = null)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Images.OrderBy(i => i.DisplayOrder))
                .Include(p => p.Variants.Where(v => v.IsActive))
                .Include(p => p.Reviews.Where(r => r.IsApproved).OrderByDescending(r => r.CreatedAt))
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive);

            if (product == null) return null;

            bool inWishlist = false;
            if (!string.IsNullOrEmpty(currentUserId))
            {
                inWishlist = await _context.WishlistItems.AnyAsync(w => w.UserId == currentUserId && w.ProductId == product.Id);
            }

            var vm = new ProductDetailVM
            {
                Id = product.Id,
                Name = product.Name,
                Slug = product.Slug,
                SKU = product.SKU,
                ShortDescription = product.ShortDescription,
                Description = product.Description,
                BasePrice = product.BasePrice,
                DiscountPrice = product.DiscountPrice,
                StockQuantity = product.StockQuantity,
                CategoryId = product.CategoryId,
                CategoryName = product.Category.Name,
                BrandName = product.Brand?.Name,
                AverageRating = product.AverageRating,
                TotalReviews = product.TotalReviews,
                IsInWishlist = inWishlist,
                Images = product.Images.Select(i => new ProductImageVM
                {
                    Id = i.Id,
                    ImageUrl = i.ImageUrl,
                    AltText = i.AltText,
                    IsPrimary = i.IsPrimary
                }).ToList(),
                Variants = product.Variants.Select(v => new ProductVariantVM
                {
                    Id = v.Id,
                    VariantName = v.VariantName,
                    SKU = v.SKU,
                    Price = v.Price,
                    StockQuantity = v.StockQuantity,
                    AttributesJson = v.AttributesJson,
                    IsActive = v.IsActive
                }).ToList(),
                Reviews = product.Reviews.Select(r => new ReviewVM
                {
                    Id = r.Id,
                    ProductId = r.ProductId,
                    UserName = r.User?.FullName ?? "Customer",
                    UserAvatar = r.User?.AvatarUrl,
                    Rating = r.Rating,
                    Title = r.Title,
                    Comment = r.Comment,
                    IsVerifiedPurchase = r.IsVerifiedPurchase,
                    CreatedAt = r.CreatedAt
                }).ToList()
            };

            // Related Products in the same category
            vm.RelatedProducts = await _context.Products
                .Include(p => p.Images)
                .Include(p => p.Category)
                .Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id && p.IsActive)
                .OrderByDescending(p => p.AverageRating)
                .Take(4)
                .Select(p => new ProductListItemVM
                {
                    Id = p.Id,
                    Name = p.Name,
                    Slug = p.Slug,
                    BasePrice = p.BasePrice,
                    DiscountPrice = p.DiscountPrice,
                    CategoryName = p.Category.Name,
                    PrimaryImageUrl = p.Images.Where(i => i.IsPrimary).Select(i => i.ImageUrl).FirstOrDefault() 
                                      ?? p.Images.Select(i => i.ImageUrl).FirstOrDefault() 
                                      ?? "/images/placeholder-product.png",
                    AverageRating = p.AverageRating,
                    TotalReviews = p.TotalReviews
                })
                .ToListAsync();

            return vm;
        }

        public async Task<ProductDetailVM?> GetProductDetailByIdAsync(int id, string? currentUserId = null)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return null;
            return await GetProductDetailBySlugAsync(product.Slug, currentUserId);
        }

        public async Task<int> CreateProductAsync(CreateEditProductVM model)
        {
            var slug = GenerateSlug(model.Name);
            int count = 1;
            var baseSlug = slug;
            while (await _context.Products.AnyAsync(p => p.Slug == slug))
            {
                slug = $"{baseSlug}-{count++}";
            }

            // Calculate effective stock quantity from variants if supplied
            int calculatedStock = model.StockQuantity;
            if (model.Variants != null && model.Variants.Count > 0)
            {
                calculatedStock = model.Variants.Sum(v => v.StockQuantity);
            }

            var product = new Product
            {
                Name = model.Name.Trim(),
                Slug = slug,
                SKU = model.SKU.Trim().ToUpper(),
                ShortDescription = model.ShortDescription,
                Description = model.Description,
                BasePrice = model.BasePrice,
                DiscountPrice = model.DiscountPrice,
                StockQuantity = calculatedStock,
                LowStockThreshold = model.LowStockThreshold,
                CategoryId = model.CategoryId,
                BrandId = model.BrandId > 0 ? model.BrandId : null,
                IsFeatured = model.IsFeatured,
                IsActive = model.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            if (!string.IsNullOrWhiteSpace(model.PrimaryImageUrl))
            {
                product.Images.Add(new ProductImage
                {
                    ImageUrl = model.PrimaryImageUrl.Trim(),
                    IsPrimary = true,
                    DisplayOrder = 1
                });
            }

            if (!string.IsNullOrWhiteSpace(model.AdditionalImageUrls))
            {
                var additional = model.AdditionalImageUrls
                    .Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(url => url.Trim())
                    .Where(url => !string.IsNullOrEmpty(url));

                int order = 2;
                foreach (var imgUrl in additional)
                {
                    product.Images.Add(new ProductImage
                    {
                        ImageUrl = imgUrl,
                        IsPrimary = false,
                        DisplayOrder = order++
                    });
                }
            }

            // Add Variants
            if (model.Variants != null && model.Variants.Count > 0)
            {
                foreach (var v in model.Variants.Where(x => !string.IsNullOrWhiteSpace(x.VariantName)))
                {
                    product.Variants.Add(new ProductVariant
                    {
                        VariantName = v.VariantName.Trim(),
                        SKU = !string.IsNullOrWhiteSpace(v.SKU) ? v.SKU.Trim().ToUpper() : $"{product.SKU}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}",
                        Price = v.Price > 0 ? v.Price : (model.DiscountPrice ?? model.BasePrice),
                        StockQuantity = v.StockQuantity >= 0 ? v.StockQuantity : 0,
                        AttributesJson = v.AttributesJson,
                        IsActive = v.IsActive,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();
            return product.Id;
        }

        public async Task UpdateProductAsync(CreateEditProductVM model)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == model.Id);

            if (product == null) return;

            // Calculate effective stock quantity from variants if supplied
            int calculatedStock = model.StockQuantity;
            if (model.Variants != null && model.Variants.Count > 0)
            {
                calculatedStock = model.Variants.Sum(v => v.StockQuantity);
            }

            product.Name = model.Name.Trim();
            product.SKU = model.SKU.Trim().ToUpper();
            product.ShortDescription = model.ShortDescription;
            product.Description = model.Description;
            product.BasePrice = model.BasePrice;
            product.DiscountPrice = model.DiscountPrice;
            product.StockQuantity = calculatedStock;
            product.LowStockThreshold = model.LowStockThreshold;
            product.CategoryId = model.CategoryId;
            product.BrandId = model.BrandId > 0 ? model.BrandId : null;
            product.IsFeatured = model.IsFeatured;
            product.IsActive = model.IsActive;
            product.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(model.PrimaryImageUrl))
            {
                var primary = product.Images.FirstOrDefault(i => i.IsPrimary);
                if (primary != null)
                {
                    primary.ImageUrl = model.PrimaryImageUrl.Trim();
                }
                else
                {
                    product.Images.Add(new ProductImage
                    {
                        ImageUrl = model.PrimaryImageUrl.Trim(),
                        IsPrimary = true,
                        DisplayOrder = 1
                    });
                }
            }

            // Sync Variants
            if (model.Variants != null)
            {
                var incomingVariantIds = model.Variants.Where(v => v.Id > 0).Select(v => v.Id).ToHashSet();
                
                // Remove variants not in the incoming list
                var toRemove = product.Variants.Where(v => !incomingVariantIds.Contains(v.Id)).ToList();
                foreach (var rem in toRemove)
                {
                    _context.ProductVariants.Remove(rem);
                }

                // Update or Add variants
                foreach (var v in model.Variants.Where(x => !string.IsNullOrWhiteSpace(x.VariantName)))
                {
                    if (v.Id > 0)
                    {
                        var existing = product.Variants.FirstOrDefault(x => x.Id == v.Id);
                        if (existing != null)
                        {
                            existing.VariantName = v.VariantName.Trim();
                            existing.SKU = !string.IsNullOrWhiteSpace(v.SKU) ? v.SKU.Trim().ToUpper() : existing.SKU;
                            existing.Price = v.Price > 0 ? v.Price : (model.DiscountPrice ?? model.BasePrice);
                            existing.StockQuantity = v.StockQuantity;
                            existing.AttributesJson = v.AttributesJson;
                            existing.IsActive = v.IsActive;
                            existing.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                    else
                    {
                        product.Variants.Add(new ProductVariant
                        {
                            VariantName = v.VariantName.Trim(),
                            SKU = !string.IsNullOrWhiteSpace(v.SKU) ? v.SKU.Trim().ToUpper() : $"{product.SKU}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}",
                            Price = v.Price > 0 ? v.Price : (model.DiscountPrice ?? model.BasePrice),
                            StockQuantity = v.StockQuantity >= 0 ? v.StockQuantity : 0,
                            AttributesJson = v.AttributesJson,
                            IsActive = v.IsActive,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeleteProductAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                product.IsDeleted = true;
                product.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<CreateEditProductVM?> GetProductForEditAsync(int id)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return null;

            return new CreateEditProductVM
            {
                Id = product.Id,
                Name = product.Name,
                Slug = product.Slug,
                SKU = product.SKU,
                ShortDescription = product.ShortDescription,
                Description = product.Description,
                BasePrice = product.BasePrice,
                DiscountPrice = product.DiscountPrice,
                StockQuantity = product.StockQuantity,
                LowStockThreshold = product.LowStockThreshold,
                CategoryId = product.CategoryId,
                BrandId = product.BrandId,
                IsFeatured = product.IsFeatured,
                IsActive = product.IsActive,
                PrimaryImageUrl = product.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl 
                                  ?? product.Images.FirstOrDefault()?.ImageUrl,
                AdditionalImageUrls = string.Join("\n", product.Images.Where(i => !i.IsPrimary).Select(i => i.ImageUrl)),
                Variants = product.Variants.Select(v => new ProductVariantInputDto
                {
                    Id = v.Id,
                    VariantName = v.VariantName,
                    SKU = v.SKU,
                    Price = v.Price,
                    StockQuantity = v.StockQuantity,
                    AttributesJson = v.AttributesJson,
                    IsActive = v.IsActive
                }).ToList()
            };
        }

        public async Task<List<ProductListItemVM>> GetAllAdminProductsAsync()
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Images)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new ProductListItemVM
                {
                    Id = p.Id,
                    Name = p.Name,
                    Slug = p.Slug,
                    SKU = p.SKU,
                    BasePrice = p.BasePrice,
                    DiscountPrice = p.DiscountPrice,
                    StockQuantity = p.StockQuantity,
                    CategoryName = p.Category.Name,
                    BrandName = p.Brand != null ? p.Brand.Name : "-",
                    PrimaryImageUrl = p.Images.Where(i => i.IsPrimary).Select(i => i.ImageUrl).FirstOrDefault() 
                                      ?? p.Images.Select(i => i.ImageUrl).FirstOrDefault() 
                                      ?? "/images/placeholder-product.png",
                    AverageRating = p.AverageRating,
                    TotalReviews = p.TotalReviews,
                    IsFeatured = p.IsFeatured
                })
                .ToListAsync();
        }

        public async Task<List<ProductQuickSearchResultDto>> QuickSearchAsync(string query, int limit = 6)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new List<ProductQuickSearchResultDto>();
            }

            var term = query.Trim().ToLower();

            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Images)
                .Where(p => p.IsActive && (
                    p.Name.ToLower().Contains(term) ||
                    p.SKU.ToLower().Contains(term) ||
                    (p.ShortDescription != null && p.ShortDescription.ToLower().Contains(term)) ||
                    p.Category.Name.ToLower().Contains(term) ||
                    (p.Brand != null && p.Brand.Name.ToLower().Contains(term))
                ))
                .OrderByDescending(p => p.AverageRating)
                .ThenByDescending(p => p.TotalReviews)
                .Take(limit)
                .Select(p => new ProductQuickSearchResultDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Slug = p.Slug,
                    CategoryName = p.Category.Name,
                    BrandName = p.Brand != null ? p.Brand.Name : null,
                    ImageUrl = p.Images.Where(i => i.IsPrimary).Select(i => i.ImageUrl).FirstOrDefault() 
                               ?? p.Images.Select(i => i.ImageUrl).FirstOrDefault() 
                               ?? "/images/placeholder-product.png",
                    Price = p.DiscountPrice.HasValue && p.DiscountPrice.Value > 0 && p.DiscountPrice.Value < p.BasePrice 
                            ? p.DiscountPrice.Value 
                            : p.BasePrice,
                    OriginalPrice = p.DiscountPrice.HasValue && p.DiscountPrice.Value > 0 && p.DiscountPrice.Value < p.BasePrice 
                            ? p.BasePrice 
                            : null,
                    DiscountPercentage = p.DiscountPrice.HasValue && p.DiscountPrice.Value > 0 && p.DiscountPrice.Value < p.BasePrice 
                            ? (int)Math.Round((1 - (p.DiscountPrice.Value / p.BasePrice)) * 100) 
                            : 0,
                    InStock = p.StockQuantity > 0,
                    Url = $"/Shop/Details/{p.Slug}"
                })
                .ToListAsync();
        }

        private static string GenerateSlug(string phrase)
        {
            string str = phrase.ToLowerInvariant();
            str = Regex.Replace(str, @"[^a-z0-9\s-]", "");
            str = Regex.Replace(str, @"\s+", " ").Trim();
            str = Regex.Replace(str, @"\s", "-");
            return str;
        }
    }
}
