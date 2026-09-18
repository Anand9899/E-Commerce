using System.Collections.Generic;
using System.Threading.Tasks;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Interfaces
{
    public interface IProductService
    {
        Task<ProductFilterVM> GetFilteredProductsAsync(ProductFilterVM filter, string? currentUserId = null);
        Task<List<ProductListItemVM>> GetFeaturedProductsAsync(int count = 8, string? currentUserId = null);
        Task<List<ProductListItemVM>> GetTrendingProductsAsync(int count = 8, string? currentUserId = null);
        Task<ProductDetailVM?> GetProductDetailBySlugAsync(string slug, string? currentUserId = null);
        Task<ProductDetailVM?> GetProductDetailByIdAsync(int id, string? currentUserId = null);
        Task<int> CreateProductAsync(CreateEditProductVM model);
        Task UpdateProductAsync(CreateEditProductVM model);
        Task DeleteProductAsync(int id);
        Task<CreateEditProductVM?> GetProductForEditAsync(int id);
        Task<List<ProductListItemVM>> GetAllAdminProductsAsync();
        Task<List<ProductQuickSearchResultDto>> QuickSearchAsync(string query, int limit = 6);
    }
}
