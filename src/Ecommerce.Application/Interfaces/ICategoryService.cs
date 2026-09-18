using System.Collections.Generic;
using System.Threading.Tasks;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Interfaces
{
    public interface ICategoryService
    {
        Task<List<CategoryVM>> GetAllCategoriesAsync();
        Task<List<CategoryVM>> GetHomeCategoriesAsync();
        Task<CategoryVM?> GetCategoryByIdAsync(int id);
        Task<CategoryVM?> GetCategoryBySlugAsync(string slug);
        Task CreateCategoryAsync(CategoryVM model);
        Task UpdateCategoryAsync(CategoryVM model);
        Task DeleteCategoryAsync(int id);
    }
}
