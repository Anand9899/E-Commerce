using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Brand> Brands => Set<Brand>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProductImage> ProductImages => Set<ProductImage>();
        public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
        public DbSet<Address> Addresses => Set<Address>();
        public DbSet<CartItem> CartItems => Set<CartItem>();
        public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<Coupon> Coupons => Set<Coupon>();
        public DbSet<CouponUsage> CouponUsages => Set<CouponUsage>();
        public DbSet<Review> Reviews => Set<Review>();
        public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();
        public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
        public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Global Query Filter for soft delete
            modelBuilder.Entity<Category>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Brand>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Product>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Address>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Order>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Coupon>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Review>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<ContactMessage>().HasQueryFilter(e => !e.IsDeleted);

            // Configure Decimal Precisions (18, 2)
            modelBuilder.Entity<ApplicationUser>().Property(u => u.WalletBalance).HasPrecision(18, 2);
            modelBuilder.Entity<Product>().Property(p => p.BasePrice).HasPrecision(18, 2);
            modelBuilder.Entity<Product>().Property(p => p.DiscountPrice).HasPrecision(18, 2);
            modelBuilder.Entity<ProductVariant>().Property(p => p.Price).HasPrecision(18, 2);
            modelBuilder.Entity<CartItem>().Property(p => p.UnitPrice).HasPrecision(18, 2);
            modelBuilder.Entity<Order>().Property(p => p.SubTotal).HasPrecision(18, 2);
            modelBuilder.Entity<Order>().Property(p => p.DiscountAmount).HasPrecision(18, 2);
            modelBuilder.Entity<Order>().Property(p => p.ShippingFee).HasPrecision(18, 2);
            modelBuilder.Entity<Order>().Property(p => p.TaxAmount).HasPrecision(18, 2);
            modelBuilder.Entity<Order>().Property(p => p.GrandTotal).HasPrecision(18, 2);
            modelBuilder.Entity<Order>().Property(p => p.WalletAmountUsed).HasPrecision(18, 2);
            modelBuilder.Entity<OrderItem>().Property(p => p.UnitPrice).HasPrecision(18, 2);
            modelBuilder.Entity<OrderItem>().Property(p => p.TotalPrice).HasPrecision(18, 2);
            modelBuilder.Entity<Payment>().Property(p => p.Amount).HasPrecision(18, 2);
            modelBuilder.Entity<Coupon>().Property(p => p.DiscountValue).HasPrecision(18, 2);
            modelBuilder.Entity<Coupon>().Property(p => p.MinOrderAmount).HasPrecision(18, 2);
            modelBuilder.Entity<Coupon>().Property(p => p.MaxDiscountAmount).HasPrecision(18, 2);
            modelBuilder.Entity<CouponUsage>().Property(p => p.DiscountAmount).HasPrecision(18, 2);
            modelBuilder.Entity<WalletTransaction>().Property(w => w.Amount).HasPrecision(18, 2);
            modelBuilder.Entity<WalletTransaction>().Property(w => w.BalanceAfter).HasPrecision(18, 2);

            // Indexes
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Slug)
                .IsUnique();

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.SKU)
                .IsUnique();

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.CategoryId);

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.IsFeatured);

            modelBuilder.Entity<Category>()
                .HasIndex(c => c.Slug)
                .IsUnique();

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.OrderNumber)
                .IsUnique();

            modelBuilder.Entity<Coupon>()
                .HasIndex(c => c.Code)
                .IsUnique();

            // Relationships
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Brand)
                .WithMany(b => b.Products)
                .HasForeignKey(p => p.BrandId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ProductImage>()
                .HasOne(i => i.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductVariant>()
                .HasOne(v => v.Product)
                .WithMany(p => p.Variants)
                .HasForeignKey(v => v.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderStatusHistory>()
                .HasOne(sh => sh.Order)
                .WithMany(o => o.StatusHistories)
                .HasForeignKey(sh => sh.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Order)
                .WithMany(o => o.Payments)
                .HasForeignKey(p => p.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CouponUsage>()
                .HasOne(cu => cu.Order)
                .WithMany()
                .HasForeignKey(cu => cu.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CouponUsage>()
                .HasOne(cu => cu.User)
                .WithMany()
                .HasForeignKey(cu => cu.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CouponUsage>()
                .HasOne(cu => cu.Coupon)
                .WithMany(c => c.CouponUsages)
                .HasForeignKey(cu => cu.CouponId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Product)
                .WithMany(p => p.Reviews)
                .HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.User)
                .WithMany(u => u.CartItems)
                .HasForeignKey(ci => ci.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Product)
                .WithMany()
                .HasForeignKey(ci => ci.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WishlistItem>()
                .HasOne(wi => wi.User)
                .WithMany(u => u.WishlistItems)
                .HasForeignKey(wi => wi.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WishlistItem>()
                .HasOne(wi => wi.Product)
                .WithMany()
                .HasForeignKey(wi => wi.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Address>()
                .HasOne(a => a.User)
                .WithMany(u => u.Addresses)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
