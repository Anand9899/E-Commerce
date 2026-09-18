using System.Collections.Generic;
using System.Threading.Tasks;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Interfaces
{
    public interface IBrandService
    {
        Task<List<BrandVM>> GetAllBrandsAsync();
        Task<BrandVM?> GetBrandByIdAsync(int id);
        Task CreateBrandAsync(BrandVM model);
        Task UpdateBrandAsync(BrandVM model);
        Task DeleteBrandAsync(int id);
    }
}
