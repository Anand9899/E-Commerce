using System.Collections.Generic;
using System.Threading.Tasks;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Interfaces
{
    public interface IAddressService
    {
        Task<List<AddressVM>> GetUserAddressesAsync(string userId);
        Task<AddressVM?> GetAddressByIdAsync(int id, string userId);
        Task<int> SaveAddressAsync(AddressVM model, string userId);
        Task SetDefaultAddressAsync(int id, string userId);
        Task DeleteAddressAsync(int id, string userId);
    }
}
