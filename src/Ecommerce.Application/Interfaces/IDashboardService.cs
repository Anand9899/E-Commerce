using System.Threading.Tasks;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Interfaces
{
    public interface IDashboardService
    {
        Task<AdminDashboardVM> GetDashboardStatsAsync();
    }
}
