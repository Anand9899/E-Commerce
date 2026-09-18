using System.Collections.Generic;

namespace Ecommerce.Application.DTOs
{
    public class AdminDashboardVM
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int TotalProducts { get; set; }
        public int TotalCustomers { get; set; }
        public int LowStockProductsCount { get; set; }
        public int PendingOrdersCount { get; set; }

        public List<OrderListVM> RecentOrders { get; set; } = new List<OrderListVM>();
        public List<ProductListItemVM> LowStockProducts { get; set; } = new List<ProductListItemVM>();
        public List<MonthlySalesData> MonthlySales { get; set; } = new List<MonthlySalesData>();
        public Dictionary<string, int> OrdersByStatus { get; set; } = new Dictionary<string, int>();
    }

    public class MonthlySalesData
    {
        public string Month { get; set; } = string.Empty; // "Jan", "Feb", etc.
        public decimal Revenue { get; set; }
        public int OrdersCount { get; set; }
    }
}
