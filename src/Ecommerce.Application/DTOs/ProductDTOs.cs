using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Application.DTOs
{
    public class ProductListItemVM
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string? ShortDescription { get; set; }
        public decimal BasePrice { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int StockQuantity { get; set; }
        public string? CategoryName { get; set; }
        public string? CategorySlug { get; set; }
        public string? BrandName { get; set; }
        public string? PrimaryImageUrl { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsInWishlist { get; set; }

        public decimal EffectivePrice => DiscountPrice.HasValue && DiscountPrice.Value > 0 && DiscountPrice.Value < BasePrice 
            ? DiscountPrice.Value 
            : BasePrice;

        public int DiscountPercentage => DiscountPrice.HasValue && DiscountPrice.Value > 0 && DiscountPrice.Value < BasePrice
            ? (int)Math.Round((1 - (DiscountPrice.Value / BasePrice)) * 100)
            : 0;
    }

    public class ProductFilterVM
    {
        public string? SearchTerm { get; set; }
        public int? CategoryId { get; set; }
        public string? CategorySlug { get; set; }
        public int? BrandId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int? MinRating { get; set; }
        public bool InStockOnly { get; set; } = false;
        public string? SortBy { get; set; } = "newest"; // newest, price_asc, price_desc, rating, popular
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);

        public List<ProductListItemVM> Products { get; set; } = new List<ProductListItemVM>();
        public List<CategoryVM> Categories { get; set; } = new List<CategoryVM>();
        public List<BrandVM> Brands { get; set; } = new List<BrandVM>();
    }

    public class ProductDetailVM
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string? ShortDescription { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int StockQuantity { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? BrandName { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public bool IsInWishlist { get; set; }

        public decimal EffectivePrice => DiscountPrice.HasValue && DiscountPrice.Value > 0 && DiscountPrice.Value < BasePrice 
            ? DiscountPrice.Value 
            : BasePrice;

        public int DiscountPercentage => DiscountPrice.HasValue && DiscountPrice.Value > 0 && DiscountPrice.Value < BasePrice
            ? (int)Math.Round((1 - (DiscountPrice.Value / BasePrice)) * 100)
            : 0;

        public List<ProductImageVM> Images { get; set; } = new List<ProductImageVM>();
        public List<ProductVariantVM> Variants { get; set; } = new List<ProductVariantVM>();
        public List<ReviewVM> Reviews { get; set; } = new List<ReviewVM>();
        public List<ProductListItemVM> RelatedProducts { get; set; } = new List<ProductListItemVM>();
    }

    public class ProductImageVM
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string? AltText { get; set; }
        public bool IsPrimary { get; set; }
    }

    public class ProductVariantVM
    {
        public int Id { get; set; }
        public string VariantName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string? AttributesJson { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class ProductVariantInputDto
    {
        public int Id { get; set; }
        public string VariantName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string? AttributesJson { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class CreateEditProductVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Product name is required")]
        public string Name { get; set; } = string.Empty;

        public string? Slug { get; set; }

        [Required(ErrorMessage = "SKU is required")]
        public string SKU { get; set; } = string.Empty;

        public string? ShortDescription { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Base price is required")]
        [Range(0.01, 1000000, ErrorMessage = "Price must be greater than 0")]
        public decimal BasePrice { get; set; }

        public decimal? DiscountPrice { get; set; }

        [Required(ErrorMessage = "Stock quantity is required")]
        [Range(0, 100000)]
        public int StockQuantity { get; set; }

        public int LowStockThreshold { get; set; } = 5;

        [Required(ErrorMessage = "Please select a category")]
        public int CategoryId { get; set; }

        public int? BrandId { get; set; }

        public bool IsFeatured { get; set; }
        public bool IsActive { get; set; } = true;

        public string? PrimaryImageUrl { get; set; }
        public string? AdditionalImageUrls { get; set; }

        public List<ProductVariantInputDto> Variants { get; set; } = new List<ProductVariantInputDto>();
    }

    public class ProductQuickSearchResultDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? CategoryName { get; set; }
        public string? BrandName { get; set; }
        public string? ImageUrl { get; set; }
        public decimal Price { get; set; }
        public decimal? OriginalPrice { get; set; }
        public int DiscountPercentage { get; set; }
        public bool InStock { get; set; }
        public string Url { get; set; } = string.Empty;
    }
}
