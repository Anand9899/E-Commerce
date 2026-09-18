using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enums;
using Ecommerce.Infrastructure.Data;

namespace Ecommerce.Infrastructure.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;

        public DashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AdminDashboardVM> GetDashboardStatsAsync()
        {
            var totalRevenue = await _context.Orders
                .Where(o => o.PaymentStatus == PaymentStatus.Paid || o.Status == OrderStatus.Delivered)
                .SumAsync(o => (decimal?)o.GrandTotal) ?? 0;

            var totalOrders = await _context.Orders.CountAsync();
            var totalProducts = await _context.Products.CountAsync(p => p.IsActive);
            var totalCustomers = await _context.Users.CountAsync();
            var lowStockCount = await _context.Products.CountAsync(p => p.IsActive && p.StockQuantity <= p.LowStockThreshold);
            var pendingOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.Confirmed);

            var recentOrders = await _context.Orders
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.OrderDate)
                .Take(6)
                .Select(o => new OrderListVM
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    OrderDate = o.OrderDate,
                    GrandTotal = o.GrandTotal,
                    Status = o.Status,
                    PaymentStatus = o.PaymentStatus,
                    PaymentMethod = o.PaymentMethod,
                    TotalItems = o.OrderItems.Sum(i => i.Quantity),
                    FirstProductImage = o.OrderItems.Select(i => i.ProductImageUrl).FirstOrDefault() ?? "/images/placeholder-product.png"
                })
                .ToListAsync();

            var lowStockProducts = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Where(p => p.IsActive && p.StockQuantity <= p.LowStockThreshold)
                .OrderBy(p => p.StockQuantity)
                .Take(5)
                .Select(p => new ProductListItemVM
                {
                    Id = p.Id,
                    Name = p.Name,
                    SKU = p.SKU,
                    BasePrice = p.BasePrice,
                    StockQuantity = p.StockQuantity,
                    CategoryName = p.Category.Name,
                    PrimaryImageUrl = p.Images.FirstOrDefault(i => i.IsPrimary) != null 
                        ? p.Images.FirstOrDefault(i => i.IsPrimary)!.ImageUrl 
                        : (p.Images.FirstOrDefault() != null ? p.Images.FirstOrDefault()!.ImageUrl : "/images/placeholder-product.png")
                })
                .ToListAsync();

            // Monthly Sales Data (Last 6 Months)
            var monthlySales = new List<MonthlySalesData>();
            var now = DateTime.UtcNow;
            for (int i = 5; i >= 0; i--)
            {
                var monthDate = now.AddMonths(-i);
                var monthStart = new DateTime(monthDate.Year, monthDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var monthEnd = monthStart.AddMonths(1);

                var rev = await _context.Orders
                    .Where(o => o.OrderDate >= monthStart && o.OrderDate < monthEnd)
                    .SumAsync(o => (decimal?)o.GrandTotal) ?? 0;

                var ordCount = await _context.Orders
                    .CountAsync(o => o.OrderDate >= monthStart && o.OrderDate < monthEnd);

                monthlySales.Add(new MonthlySalesData
                {
                    Month = monthDate.ToString("MMM yyyy"),
                    Revenue = rev,
                    OrdersCount = ordCount
                });
            }

            var ordersByStatus = await _context.Orders
                .GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);

            return new AdminDashboardVM
            {
                TotalRevenue = totalRevenue,
                TotalOrders = totalOrders,
                TotalProducts = totalProducts,
                TotalCustomers = totalCustomers,
                LowStockProductsCount = lowStockCount,
                PendingOrdersCount = pendingOrders,
                RecentOrders = recentOrders,
                LowStockProducts = lowStockProducts,
                MonthlySales = monthlySales,
                OrdersByStatus = ordersByStatus
            };
        }
    }
}
