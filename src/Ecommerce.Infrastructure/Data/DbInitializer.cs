using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enums;

namespace Ecommerce.Infrastructure.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager, 
            RoleManager<ApplicationRole> roleManager)
        {
            // Apply migrations automatically if needed
            try
            {
                await context.Database.MigrateAsync();
            }
            catch { }

            // Ensure ContactMessages table exists
            try
            {
                await context.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ContactMessages')
                    BEGIN
                        CREATE TABLE [dbo].[ContactMessages] (
                            [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            [Name] NVARCHAR(100) NOT NULL,
                            [Email] NVARCHAR(256) NOT NULL,
                            [Phone] NVARCHAR(50) NULL,
                            [Subject] NVARCHAR(200) NOT NULL,
                            [Message] NVARCHAR(MAX) NOT NULL,
                            [IsRead] BIT NOT NULL DEFAULT 0,
                            [IsReplied] BIT NOT NULL DEFAULT 0,
                            [AdminNotes] NVARCHAR(MAX) NULL,
                            [RepliedBy] NVARCHAR(100) NULL,
                            [RepliedAt] DATETIME2 NULL,
                            [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                            [UpdatedAt] DATETIME2 NULL,
                        );
                    END

                    -- Ensure Orders table has return, refund, logistics and wallet tracking columns
                    IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Orders')
                    BEGIN
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'ReturnReason')
                            ALTER TABLE [dbo].[Orders] ADD [ReturnReason] NVARCHAR(MAX) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'ReturnComments')
                            ALTER TABLE [dbo].[Orders] ADD [ReturnComments] NVARCHAR(MAX) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'ReturnRefundPaymentDetails')
                            ALTER TABLE [dbo].[Orders] ADD [ReturnRefundPaymentDetails] NVARCHAR(MAX) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'ReturnRequestedAt')
                            ALTER TABLE [dbo].[Orders] ADD [ReturnRequestedAt] DATETIME2 NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'ReturnActionAt')
                            ALTER TABLE [dbo].[Orders] ADD [ReturnActionAt] DATETIME2 NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'RefundAmount')
                            ALTER TABLE [dbo].[Orders] ADD [RefundAmount] DECIMAL(18,2) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'RefundReferenceId')
                            ALTER TABLE [dbo].[Orders] ADD [RefundReferenceId] NVARCHAR(100) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'RefundAdminNotes')
                            ALTER TABLE [dbo].[Orders] ADD [RefundAdminNotes] NVARCHAR(MAX) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'CourierPartner')
                            ALTER TABLE [dbo].[Orders] ADD [CourierPartner] NVARCHAR(MAX) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'EstimatedDeliveryDate')
                            ALTER TABLE [dbo].[Orders] ADD [EstimatedDeliveryDate] DATETIME2 NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'ShippedAt')
                            ALTER TABLE [dbo].[Orders] ADD [ShippedAt] DATETIME2 NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'OutForDeliveryAt')
                            ALTER TABLE [dbo].[Orders] ADD [OutForDeliveryAt] DATETIME2 NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'DeliveredAt')
                            ALTER TABLE [dbo].[Orders] ADD [DeliveredAt] DATETIME2 NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'CurrentLocation')
                            ALTER TABLE [dbo].[Orders] ADD [CurrentLocation] NVARCHAR(MAX) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'WalletAmountUsed')
                            ALTER TABLE [dbo].[Orders] ADD [WalletAmountUsed] DECIMAL(18,2) NOT NULL DEFAULT 0;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'LoyaltyPointsEarned')
                            ALTER TABLE [dbo].[Orders] ADD [LoyaltyPointsEarned] INT NOT NULL DEFAULT 0;
                    END

                    -- Ensure AspNetUsers table has WalletBalance and LoyaltyPoints
                    IF EXISTS (SELECT * FROM sys.tables WHERE name = 'AspNetUsers')
                    BEGIN
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'WalletBalance')
                            ALTER TABLE [dbo].[AspNetUsers] ADD [WalletBalance] DECIMAL(18,2) NOT NULL DEFAULT 500.00;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'LoyaltyPoints')
                            ALTER TABLE [dbo].[AspNetUsers] ADD [LoyaltyPoints] INT NOT NULL DEFAULT 100;
                    END

                    -- Ensure WalletTransactions table exists
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'WalletTransactions')
                    BEGIN
                        CREATE TABLE [dbo].[WalletTransactions] (
                            [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            [UserId] NVARCHAR(450) NOT NULL,
                            [Amount] DECIMAL(18,2) NOT NULL,
                            [Type] INT NOT NULL,
                            [Description] NVARCHAR(MAX) NOT NULL,
                            [BalanceAfter] DECIMAL(18,2) NOT NULL,
                            [OrderId] INT NULL,
                            [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                            [UpdatedAt] DATETIME2 NULL,
                            [IsDeleted] BIT NOT NULL DEFAULT 0,
                            CONSTRAINT [FK_WalletTransactions_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE
                        );
                    END
                ");
            }
            catch { }

            // 1. Seed Roles
            string[] roles = new[] { "Admin", "Customer", "Seller" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new ApplicationRole(role)
                    {
                        Description = $"{role} Role for E-Commerce platform"
                    });
                }
            }

            // 2. Seed Default Admin User
            string adminEmail = "admmin123@gmail.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                var oldAdmin = await userManager.FindByEmailAsync("admin@ecommerce.com");
                if (oldAdmin != null)
                {
                    oldAdmin.Email = adminEmail;
                    oldAdmin.UserName = adminEmail;
                    oldAdmin.NormalizedEmail = adminEmail.ToUpper();
                    oldAdmin.NormalizedUserName = adminEmail.ToUpper();
                    await userManager.UpdateAsync(oldAdmin);
                    var token = await userManager.GeneratePasswordResetTokenAsync(oldAdmin);
                    await userManager.ResetPasswordAsync(oldAdmin, token, "Admin123");
                    adminUser = oldAdmin;
                }
                else
                {
                    adminUser = new ApplicationUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        FullName = "Admin Store Manager",
                        PhoneNumber = "+91 9876543210",
                        EmailConfirmed = true,
                        PhoneNumberConfirmed = true,
                        PlainPassword = "Admin123",
                        AvatarUrl = "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=150&auto=format&fit=crop&q=80"
                    };
                    var result = await userManager.CreateAsync(adminUser, "Admin123");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(adminUser, "Admin");
                    }
                }
            }
            else
            {
                adminUser.PlainPassword = "Admin123";
                await userManager.UpdateAsync(adminUser);
                var token = await userManager.GeneratePasswordResetTokenAsync(adminUser);
                await userManager.ResetPasswordAsync(adminUser, token, "Admin123");
                if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // 3. Seed Default Customer User
            string customerEmail = "customer@ecommerce.com";
            var customerUser = await userManager.FindByEmailAsync(customerEmail);
            if (customerUser == null)
            {
                customerUser = new ApplicationUser
                {
                    UserName = customerEmail,
                    Email = customerEmail,
                    FullName = "Anand Mishra",
                    PhoneNumber = "+91 9123456780",
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true,
                    PlainPassword = "Customer@123",
                    AvatarUrl = "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=150&auto=format&fit=crop&q=80"
                };
                var result = await userManager.CreateAsync(customerUser, "Customer@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(customerUser, "Customer");

                    // Seed default customer address
                    await context.Addresses.AddAsync(new Address
                    {
                        UserId = customerUser.Id,
                        FullName = "Anand Mishra",
                        PhoneNumber = "+91 9123456780",
                        AddressLine1 = "Flat 402, Sunshine Heights, MG Road",
                        AddressLine2 = "Near City Mall, Sector 18",
                        City = "Noida",
                        State = "Uttar Pradesh",
                        PostalCode = "201301",
                        Country = "India",
                        IsDefault = true,
                        CreatedAt = DateTime.UtcNow
                    });
                    await context.SaveChangesAsync();
                }
            }

            // 4. Seed Categories
            var categoriesToSeed = new List<Category>
            {
                new() { Name = "Men's Fashion", Slug = "mens-fashion", Description = "Men's Clothing, Shirts, T-Shirts, Jeans, Suits & Ethnic Wear", ImageUrl = "https://images.unsplash.com/photo-1617137984095-74e4e5e3613f?w=600&auto=format&fit=crop&q=80", IconClass = "bi-person-standing", DisplayOrder = 1, ShowOnHome = true },
                new() { Name = "Women's Fashion", Slug = "womens-fashion", Description = "Women's Clothing, Dresses, Sarees, Kurtis, Tops & Jeans", ImageUrl = "https://images.unsplash.com/photo-1483985988355-763728e1935b?w=600&auto=format&fit=crop&q=80", IconClass = "bi-person-standing-dress", DisplayOrder = 2, ShowOnHome = true },
                new() { Name = "Electronics", Slug = "electronics", Description = "Laptops, Smartphones, Audio & Smart Gadgets", ImageUrl = "https://images.unsplash.com/photo-1498049794561-7780e7231661?w=600&auto=format&fit=crop&q=80", IconClass = "bi-laptop", DisplayOrder = 3, ShowOnHome = true },
                new() { Name = "Home & Kitchen", Slug = "home-kitchen", Description = "Modern Appliances, Decor & Furniture", ImageUrl = "https://images.unsplash.com/photo-1556911220-e15b29be8c8f?w=600&auto=format&fit=crop&q=80", IconClass = "bi-house-door", DisplayOrder = 4, ShowOnHome = true },
                new() { Name = "Beauty & Personal Care", Slug = "beauty-personal-care", Description = "Skincare, Perfumes, Makeup & Grooming", ImageUrl = "https://images.unsplash.com/photo-1522335789203-aabd1fc54bc9?w=600&auto=format&fit=crop&q=80", IconClass = "bi-stars", DisplayOrder = 5, ShowOnHome = true },
                new() { Name = "Sports & Fitness", Slug = "sports-fitness", Description = "Gym Gear, Activewear & Sporting Goods", ImageUrl = "https://images.unsplash.com/photo-1517838277536-f5f99be501cd?w=600&auto=format&fit=crop&q=80", IconClass = "bi-trophy", DisplayOrder = 6, ShowOnHome = true },
                new() { Name = "Books & Stationery", Slug = "books-stationery", Description = "Bestselling Novels, Tech Guides & Planners", ImageUrl = "https://images.unsplash.com/photo-1512820790803-83ca734da794?w=600&auto=format&fit=crop&q=80", IconClass = "bi-book", DisplayOrder = 7, ShowOnHome = true }
            };

            foreach (var cat in categoriesToSeed)
            {
                var existingCat = await context.Categories.FirstOrDefaultAsync(c => c.Slug == cat.Slug);
                if (existingCat == null)
                {
                    await context.Categories.AddAsync(cat);
                }
                else
                {
                    existingCat.Name = cat.Name;
                    existingCat.Description = cat.Description;
                    existingCat.ImageUrl = cat.ImageUrl;
                    existingCat.IconClass = cat.IconClass;
                    existingCat.DisplayOrder = cat.DisplayOrder;
                    existingCat.ShowOnHome = cat.ShowOnHome;
                }
            }
            await context.SaveChangesAsync();

            // 5. Seed Brands
            var brandList = new List<Brand>
            {
                new() { Name = "Apple", Slug = "apple", LogoUrl = "https://images.unsplash.com/photo-1611186871348-b1ce696e52c9?w=200&auto=format&fit=crop&q=80" },
                new() { Name = "Sony", Slug = "sony", LogoUrl = "https://images.unsplash.com/photo-1526738549149-8e07eca6c147?w=200&auto=format&fit=crop&q=80" },
                new() { Name = "Samsung", Slug = "samsung", LogoUrl = "https://images.unsplash.com/photo-1610945265064-0e34e5519bbf?w=200&auto=format&fit=crop&q=80" },
                new() { Name = "Nike", Slug = "nike", LogoUrl = "https://images.unsplash.com/photo-1542291026-7eec264c27ff?w=200&auto=format&fit=crop&q=80" },
                new() { Name = "Adidas", Slug = "adidas", LogoUrl = "https://images.unsplash.com/photo-1518002171953-a080ee817e1f?w=200&auto=format&fit=crop&q=80" },
                new() { Name = "Philips", Slug = "philips", LogoUrl = "https://images.unsplash.com/photo-1583394838336-acd977736f90?w=200&auto=format&fit=crop&q=80" },
                new() { Name = "Dell", Slug = "dell", LogoUrl = "https://images.unsplash.com/photo-1588872657578-7efd1f1555ed?w=200&auto=format&fit=crop&q=80" },
                new() { Name = "Bose", Slug = "bose", LogoUrl = "https://images.unsplash.com/photo-1546435770-a3e426bf472b?w=200&auto=format&fit=crop&q=80" },
                new() { Name = "Dyson", Slug = "dyson", LogoUrl = "https://images.unsplash.com/photo-1527443224154-c4a3942d3acf?w=200&auto=format&fit=crop&q=80" },
                new() { Name = "Puma", Slug = "puma", LogoUrl = "https://images.unsplash.com/photo-1608231387042-66d1773070a5?w=200&auto=format&fit=crop&q=80" }
            };

            foreach (var b in brandList)
            {
                if (!await context.Brands.AnyAsync(x => x.Slug == b.Slug))
                {
                    await context.Brands.AddAsync(b);
                }
            }
            await context.SaveChangesAsync();

            // 6. Seed Products (Only if database has no products yet)
            if (!await context.Products.AnyAsync())
            {
                var catElectronics = await context.Categories.FirstAsync(c => c.Slug == "electronics");
                var catMensFashion = await context.Categories.FirstAsync(c => c.Slug == "mens-fashion");
                var catWomensFashion = await context.Categories.FirstAsync(c => c.Slug == "womens-fashion");
                var catHome = await context.Categories.FirstAsync(c => c.Slug == "home-kitchen");
                var catBeauty = await context.Categories.FirstAsync(c => c.Slug == "beauty-personal-care");
                var catSports = await context.Categories.FirstAsync(c => c.Slug == "sports-fitness");
                var catBooks = await context.Categories.FirstAsync(c => c.Slug == "books-stationery");

            var brandApple = await context.Brands.FirstOrDefaultAsync(b => b.Slug == "apple");
            var brandSony = await context.Brands.FirstOrDefaultAsync(b => b.Slug == "sony");
            var brandSamsung = await context.Brands.FirstOrDefaultAsync(b => b.Slug == "samsung");
            var brandNike = await context.Brands.FirstOrDefaultAsync(b => b.Slug == "nike");
            var brandAdidas = await context.Brands.FirstOrDefaultAsync(b => b.Slug == "adidas");
            var brandPhilips = await context.Brands.FirstOrDefaultAsync(b => b.Slug == "philips");
            var brandDell = await context.Brands.FirstOrDefaultAsync(b => b.Slug == "dell");
            var brandBose = await context.Brands.FirstOrDefaultAsync(b => b.Slug == "bose");
            var brandDyson = await context.Brands.FirstOrDefaultAsync(b => b.Slug == "dyson");
            var brandPuma = await context.Brands.FirstOrDefaultAsync(b => b.Slug == "puma");

            var allProducts = new List<Product>
            {
                // ==========================================
                // CATEGORY 1: ELECTRONICS (10 Products)
                // ==========================================
                new()
                {
                    Name = "Apple MacBook Pro 14\" M3 Chip",
                    Slug = "apple-macbook-pro-14-m3-chip",
                    SKU = "APL-MBP14-M3",
                    ShortDescription = "Supercharged by M3 Pro. Liquid Retina XDR display, up to 18 hours battery life.",
                    Description = "The 14-inch MacBook Pro blasts forward with M3, an incredibly advanced chip that brings serious speed and capability. Liquid Retina XDR display and exceptional battery life.",
                    BasePrice = 169900m,
                    DiscountPrice = 154900m,
                    StockQuantity = 15,
                    LowStockThreshold = 3,
                    IsFeatured = true,
                    IsActive = true,
                    AverageRating = 4.9,
                    TotalReviews = 24,
                    CategoryId = catElectronics.Id,
                    BrandId = brandApple?.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1517336714731-489689fd1ca8?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 },
                        new() { ImageUrl = "https://images.unsplash.com/photo-1611186871348-b1ce696e52c9?w=800&auto=format&fit=crop&q=80", IsPrimary = false, DisplayOrder = 2 }
                    },
                    Variants = new List<ProductVariant>
                    {
                        new() { VariantName = "512GB SSD / 8GB RAM", SKU = "MBP-14-512", Price = 154900m, StockQuantity = 10 },
                        new() { VariantName = "1TB SSD / 16GB RAM", SKU = "MBP-14-1TB", Price = 189900m, StockQuantity = 5 }
                    }
                },
                new()
                {
                    Name = "Sony WH-1000XM5 Wireless Headphones",
                    Slug = "sony-wh-1000xm5-wireless-headphones",
                    SKU = "SNY-WH1000XM5-BLK",
                    ShortDescription = "Industry-leading noise canceling with dual processors and 8 microphones.",
                    Description = "The WH-1000XM5 headphones rewrite the rules for distraction-free listening with unprecedented active noise cancellation and crystal-clear call quality.",
                    BasePrice = 34990m,
                    DiscountPrice = 26990m,
                    StockQuantity = 28,
                    LowStockThreshold = 5,
                    IsFeatured = true,
                    IsActive = true,
                    AverageRating = 4.8,
                    TotalReviews = 42,
                    CategoryId = catElectronics.Id,
                    BrandId = brandSony?.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 },
                        new() { ImageUrl = "https://images.unsplash.com/photo-1484704849700-f032a568e944?w=800&auto=format&fit=crop&q=80", IsPrimary = false, DisplayOrder = 2 }
                    }
                },
                new()
                {
                    Name = "Samsung Galaxy S24 Ultra 5G AI Smartphone",
                    Slug = "samsung-galaxy-s24-ultra-5g",
                    SKU = "SAM-S24U-512-GRY",
                    ShortDescription = "Galaxy AI is here. 200MP camera, Snapdragon 8 Gen 3, S-Pen included.",
                    Description = "Meet Galaxy S24 Ultra, the ultimate form of Galaxy Ultra with a new titanium exterior and a 6.8-inch flat display. It's an absolute marvel of design with built-in Galaxy AI.",
                    BasePrice = 134999m,
                    DiscountPrice = 124999m,
                    StockQuantity = 20,
                    LowStockThreshold = 4,
                    IsFeatured = true,
                    IsActive = true,
                    AverageRating = 4.7,
                    TotalReviews = 39,
                    CategoryId = catElectronics.Id,
                    BrandId = brandSamsung?.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1610945265064-0e34e5519bbf?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Apple Watch Ultra 2 GPS + Cellular",
                    Slug = "apple-watch-ultra-2-gps-cellular",
                    SKU = "APL-WTC-ULT2-49",
                    ShortDescription = "The most rugged and capable Apple Watch. 49mm titanium case.",
                    Description = "The ultimate sports and adventure watch. Powered by S9 SiP, with a brilliant 3000-nit display, double tap gesture, and precision dual-frequency GPS.",
                    BasePrice = 89900m,
                    DiscountPrice = 84900m,
                    StockQuantity = 12,
                    LowStockThreshold = 2,
                    IsFeatured = false,
                    IsActive = true,
                    AverageRating = 4.9,
                    TotalReviews = 18,
                    CategoryId = catElectronics.Id,
                    BrandId = brandApple?.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1579586337278-3befd40fd17a?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Dell XPS 15 OLED 4K Touch Laptop",
                    Slug = "dell-xps-15-oled-4k-touch-laptop",
                    SKU = "DEL-XPS15-OLED",
                    ShortDescription = "13th Gen Intel Core i9, RTX 4070, 32GB RAM, 1TB SSD, 3.5K OLED Touch.",
                    Description = "Immerse yourself in content with stunning high-resolution, color-rich panels and expansive viewing space to keep you productive.",
                    BasePrice = 249990m,
                    DiscountPrice = 229990m,
                    StockQuantity = 8,
                    LowStockThreshold = 2,
                    IsFeatured = false,
                    IsActive = true,
                    AverageRating = 4.6,
                    TotalReviews = 14,
                    CategoryId = catElectronics.Id,
                    BrandId = brandDell?.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1588872657578-7efd1f1555ed?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Bose QuietComfort Ultra Earbuds",
                    Slug = "bose-quietcomfort-ultra-earbuds",
                    SKU = "BOS-QCU-EAR-BLK",
                    ShortDescription = "Spatial audio noise cancelling earbuds with CustomTune technology.",
                    Description = "Breakthrough spatial audio for more immersive listening that makes your music feel realer than ever before. World-class noise cancellation tailored to your ears.",
                    BasePrice = 25900m,
                    DiscountPrice = 21900m,
                    StockQuantity = 30,
                    LowStockThreshold = 5,
                    IsFeatured = false,
                    IsActive = true,
                    AverageRating = 4.7,
                    TotalReviews = 27,
                    CategoryId = catElectronics.Id,
                    BrandId = brandBose?.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1590658268037-6bf12165a8df?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Sony PlayStation 5 Slim Digital Console",
                    Slug = "sony-playstation-5-slim-digital-console",
                    SKU = "SNY-PS5-SLM-DIG",
                    ShortDescription = "1TB SSD Storage, Ultra-High Speed Custom SSD, Ray Tracing, 4K Gaming.",
                    Description = "Experience lightning fast loading with an ultra-high speed SSD, deeper immersion with support for haptic feedback, adaptive triggers, and 3D Audio.",
                    BasePrice = 44990m,
                    DiscountPrice = 39990m,
                    StockQuantity = 18,
                    LowStockThreshold = 3,
                    IsFeatured = true,
                    IsActive = true,
                    AverageRating = 4.9,
                    TotalReviews = 84,
                    CategoryId = catElectronics.Id,
                    BrandId = brandSony?.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1606813907291-d86efa9b94db?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Apple iPad Air M2 11-inch Wi-Fi 128GB",
                    Slug = "apple-ipad-air-m2-11-inch",
                    SKU = "APL-IPD-AIR11-BLU",
                    ShortDescription = "Fresh design, stunning Liquid Retina display, powered by Apple M2 chip.",
                    Description = "The redesigned 11-inch iPad Air is supercharged by the astonishingly fast Apple M2 chip. It features a gorgeous Liquid Retina display and new landscape front camera.",
                    BasePrice = 59900m,
                    DiscountPrice = 55900m,
                    StockQuantity = 22,
                    LowStockThreshold = 4,
                    IsFeatured = false,
                    IsActive = true,
                    AverageRating = 4.8,
                    TotalReviews = 21,
                    CategoryId = catElectronics.Id,
                    BrandId = brandApple?.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1544244015-0df4b3ffc6b0?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Samsung 34\" Odyssey OLED G8 Curved Gaming Monitor",
                    Slug = "samsung-34-odyssey-oled-g8-curved-gaming-monitor",
                    SKU = "SAM-MON-G8-34",
                    ShortDescription = "0.03ms response time, 175Hz refresh rate, Neo Quantum Processor.",
                    Description = "Mesmerizing OLED quality with 0.03ms response time and 175Hz refresh rate brings gaming worlds to life with intense colors and deeper blacks.",
                    BasePrice = 119999m,
                    DiscountPrice = 94999m,
                    StockQuantity = 7,
                    LowStockThreshold = 2,
                    IsFeatured = false,
                    IsActive = true,
                    AverageRating = 4.7,
                    TotalReviews = 11,
                    CategoryId = catElectronics.Id,
                    BrandId = brandSamsung?.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1527443224154-c4a3942d3acf?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Apple AirPods Pro (2nd Generation) USB-C",
                    Slug = "apple-airpods-pro-2nd-gen-usbc",
                    SKU = "APL-APP2-USBC",
                    ShortDescription = "Up to 2x more Active Noise Cancellation, Transparency mode, MagSafe case.",
                    Description = "AirPods Pro are powered by the Apple-designed H2 chip, pushing advanced audio performance even further. Featuring USB-C MagSafe charging case.",
                    BasePrice = 24900m,
                    DiscountPrice = 20990m,
                    StockQuantity = 40,
                    LowStockThreshold = 8,
                    IsFeatured = true,
                    IsActive = true,
                    AverageRating = 4.8,
                    TotalReviews = 53,
                    CategoryId = catElectronics.Id,
                    BrandId = brandApple?.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1600294037681-c80b4cb5b434?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },

                // ==========================================
                // CATEGORY 2: MEN'S FASHION & CLOTHING (10 Products)
                // ==========================================
                new()
                {
                    Name = "Tommy Hilfiger Classic Oxford Button-Down Shirt",
                    Slug = "tommy-hilfiger-classic-oxford-shirt",
                    SKU = "TH-OXF-WHT",
                    ShortDescription = "100% organic cotton oxford shirt with button-down collar and signature flag.",
                    Description = "Crisp, breathable, and versatile. Tailored in pure organic cotton with a refined regular fit that looks sharp buttoned up or worn open over a tee.",
                    BasePrice = 5499m,
                    DiscountPrice = 3999m,
                    StockQuantity = 45,
                    LowStockThreshold = 8,
                    IsFeatured = true,
                    IsActive = true,
                    AverageRating = 4.6,
                    TotalReviews = 27,
                    CategoryId = catMensFashion.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1602810318383-e386cc2a3ccf?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    },
                    Variants = new List<ProductVariant>
                    {
                        new() { VariantName = "M / White", SKU = "TH-OXF-WHT-M", Price = 3999m, StockQuantity = 15 },
                        new() { VariantName = "L / White", SKU = "TH-OXF-WHT-L", Price = 3999m, StockQuantity = 20 },
                        new() { VariantName = "XL / White", SKU = "TH-OXF-WHT-XL", Price = 3999m, StockQuantity = 10 }
                    }
                },
                new()
                {
                    Name = "Levi's 511 Slim Fit Stretch Denim Jeans",
                    Slug = "levis-511-slim-fit-stretch-denim-jeans",
                    SKU = "LEV-511-SLM-BLU",
                    ShortDescription = "A modern slim with room to move. Added stretch for all-day comfort.",
                    Description = "The definitive slim jeans. Narrow through the thigh and leg opening, perfectly calibrated to look good with everything.",
                    BasePrice = 3999m,
                    DiscountPrice = 2799m,
                    StockQuantity = 60,
                    LowStockThreshold = 12,
                    IsFeatured = true,
                    IsActive = true,
                    AverageRating = 4.6,
                    TotalReviews = 44,
                    CategoryId = catMensFashion.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1542272604-780c96856592?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    },
                    Variants = new List<ProductVariant>
                    {
                        new() { VariantName = "32 / Indigo Blue", SKU = "LEV-511-32", Price = 2799m, StockQuantity = 25 },
                        new() { VariantName = "34 / Indigo Blue", SKU = "LEV-511-34", Price = 2799m, StockQuantity = 20 },
                        new() { VariantName = "36 / Indigo Blue", SKU = "LEV-511-36", Price = 2799m, StockQuantity = 15 }
                    }
                },
                new()
                {
                    Name = "Adidas Originals Adicolor Classics Trefoil Hoodie",
                    Slug = "adidas-originals-trefoil-hoodie",
                    SKU = "ADS-HOOD-BLK",
                    ShortDescription = "An everyday fleece pullover hoodie rooted in adidas heritage.",
                    Description = "Pure comfort and timeless sportswear heritage meet in this cozy cotton-blend hoodie. Ribbed cuffs and hem seal in warmth.",
                    BasePrice = 5999m,
                    DiscountPrice = 3799m,
                    StockQuantity = 50,
                    LowStockThreshold = 10,
                    IsFeatured = false,
                    IsActive = true,
                    AverageRating = 4.5,
                    TotalReviews = 19,
                    CategoryId = catMensFashion.Id,
                    BrandId = brandAdidas?.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1556905055-8f358a7a47b2?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Zara Textured Tailored Casual Linen Blazer",
                    Slug = "zara-textured-tailored-casual-blazer",
                    SKU = "ZAR-BLZ-NVY",
                    ShortDescription = "Single-breasted relaxed blazer crafted in breathable linen-cotton blend.",
                    Description = "Sophisticated lightweight blazer featuring notched lapels, patch pockets, and unlined interior for effortless everyday style.",
                    BasePrice = 7990m,
                    DiscountPrice = 5490m,
                    StockQuantity = 25,
                    LowStockThreshold = 5,
                    IsFeatured = true,
                    IsActive = true,
                    AverageRating = 4.7,
                    TotalReviews = 22,
                    CategoryId = catMensFashion.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1507679799987-c73779587ccf?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Manyavar Royal Silk Blend Festive Kurta Pyjama Set",
                    Slug = "manyavar-royal-silk-festive-kurta-set",
                    SKU = "MNY-KRT-GLD",
                    ShortDescription = "Art silk jacquard patterned festive kurta with churidar pyjama in rich champagne gold.",
                    Description = "Exude regal charm during weddings and festivals with this handcrafted mandarin collar jacquard silk kurta ensemble.",
                    BasePrice = 6999m,
                    DiscountPrice = 4999m,
                    StockQuantity = 35,
                    LowStockThreshold = 6,
                    IsFeatured = true,
                    IsActive = true,
                    AverageRating = 4.8,
                    TotalReviews = 38,
                    CategoryId = catMensFashion.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1617137984095-74e4e5e3613f?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Nike Sportswear Club Fleece Joggers",
                    Slug = "nike-sportswear-club-fleece-joggers",
                    SKU = "NKE-JOG-GRY",
                    ShortDescription = "Soft brushed fleece joggers with ribbed cuffs and elastic drawcord waistband.",
                    Description = "A closet staple, the Nike Sportswear Club Fleece Joggers combine classic style with the soft comfort of fleece for an elevated everyday look.",
                    BasePrice = 3495m,
                    DiscountPrice = 2495m,
                    StockQuantity = 50,
                    LowStockThreshold = 10,
                    IsFeatured = false,
                    IsActive = true,
                    AverageRating = 4.5,
                    TotalReviews = 31,
                    CategoryId = catMensFashion.Id,
                    BrandId = brandNike?.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1552902865-b72c031ac5ea?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "US Polo Assn. Classic Pique Cotton Polo T-Shirt",
                    Slug = "us-polo-classic-pique-cotton-polo",
                    SKU = "USP-POLO-NAV",
                    ShortDescription = "100% breathable pique combed cotton polo with embroidered crest.",
                    Description = "A timeless casual essential crafted from premium combed cotton pique. Features ribbed collar, two-button placket and regular fit.",
                    BasePrice = 2299m,
                    DiscountPrice = 1499m,
                    StockQuantity = 55,
                    LowStockThreshold = 10,
                    IsFeatured = false,
                    IsActive = true,
                    AverageRating = 4.6,
                    TotalReviews = 49,
                    CategoryId = catMensFashion.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1581655353564-df123a1eb820?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "H&M Relaxed Fit French Terry Sweatshirt",
                    Slug = "hm-relaxed-fit-french-terry-sweatshirt",
                    SKU = "HM-SWT-BEG",
                    ShortDescription = "Heavyweight organic cotton French terry sweatshirt with dropped shoulders.",
                    Description = "Long sleeve sweatshirt in soft, heavyweight French terry. Relaxed fit with dropped shoulders and ribbing around the neckline, cuffs, and hem.",
                    BasePrice = 2499m,
                    DiscountPrice = 1799m,
                    StockQuantity = 55,
                    LowStockThreshold = 10,
                    IsFeatured = false,
                    IsActive = true,
                    AverageRating = 4.4,
                    TotalReviews = 16,
                    CategoryId = catMensFashion.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1509631179647-0177331693ae?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Roadmaster Quilted Lightweight Bomber Jacket",
                    Slug = "roadmaster-quilted-bomber-jacket",
                    SKU = "RDM-JKT-OLV",
                    ShortDescription = "Water-resistant quilted nylon jacket with thermal insulation and zip closure.",
                    Description = "Stay warm and look sharp with this modern bomber jacket featuring diamond quilting, ribbed stand collar, and windproof inner lining.",
                    BasePrice = 4999m,
                    DiscountPrice = 3299m,
                    StockQuantity = 30,
                    LowStockThreshold = 5,
                    IsFeatured = true,
                    IsActive = true,
                    AverageRating = 4.7,
                    TotalReviews = 33,
                    CategoryId = catMensFashion.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1544441893-675973e31985?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Puma Men's Slim Fit Chino Trousers",
                    Slug = "puma-mens-slim-fit-chino-trousers",
                    SKU = "PUM-CHINO-KHK",
                    ShortDescription = "Stretch cotton twill casual chinos with 4-pocket styling.",
                    Description = "Clean, versatile and comfortable for work or weekends. Tailored with a modern slim profile and added elastane for freedom of movement.",
                    BasePrice = 3499m,
                    DiscountPrice = 2199m,
                    StockQuantity = 40,
                    LowStockThreshold = 8,
                    IsFeatured = false,
                    IsActive = true,
                    AverageRating = 4.5,
                    TotalReviews = 25,
                    CategoryId = catMensFashion.Id,
                    BrandId = brandPuma?.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1479064555552-3ef4979f8908?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },

                // ==========================================
                // CATEGORY 3: WOMEN'S FASHION & CLOTHING (10 Products)
                // ==========================================
                new()
                {
                    Name = "Vero Moda Floral Print Bohemian Maxi Dress",
                    Slug = "vero-moda-floral-boho-maxi-dress",
                    SKU = "VRM-MAXI-FLR",
                    ShortDescription = "Flowy chiffon floral maxi dress with ruffle detailing and cinched waist.",
                    Description = "Step out in effortless elegance. This romantic floor-length maxi dress features a vibrant botanical print, tiered ruffle skirt, and breathable lightweight fabric.",
                    BasePrice = 4999m,
                    DiscountPrice = 2999m,
                    StockQuantity = 45,
                    LowStockThreshold = 8,
                    IsFeatured = true,
                    IsActive = true,
                    AverageRating = 4.8,
                    TotalReviews = 47,
                    CategoryId = catWomensFashion.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1572804013309-59a88b7e92f1?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    },
                    Variants = new List<ProductVariant>
                    {
                        new() { VariantName = "S / Floral", SKU = "VRM-MAXI-S", Price = 2999m, StockQuantity = 15 },
                        new() { VariantName = "M / Floral", SKU = "VRM-MAXI-M", Price = 2999m, StockQuantity = 20 },
                        new() { VariantName = "L / Floral", SKU = "VRM-MAXI-L", Price = 2999m, StockQuantity = 10 }
                    }
                },
                new()
                {
                    Name = "Libas Embroidered Anarkali Kurti & Palazzos Set",
                    Slug = "libas-embroidered-anarkali-kurti-palazzo-set",
                    SKU = "LBS-ANR-TEA",
                    ShortDescription = "Teal blue chanderi silk embroidered Anarkali kurti set with dupatta.",
                    Description = "Traditional ethnic grandeur meets modern silhouettes. Features intricate zari yoke embroidery, flared Anarkali hemline, coordinating wide-leg palazzos, and organza dupatta.",
                    BasePrice = 5999m,
                    DiscountPrice = 3499m,
                    StockQuantity = 40,
                    LowStockThreshold = 7,
                    IsFeatured = true,
                    IsActive = true,
                    AverageRating = 4.9,
                    TotalReviews = 56,
                    CategoryId = catWomensFashion.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1610030469983-98e550d6193c?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Levi's 721 High Rise Skinny Fit Stretch Jeans",
                    Slug = "levis-721-high-rise-skinny-jeans",
                    SKU = "LEV-721-SKN-BLK",
                    ShortDescription = "High-rise figure-flattering skinny jeans with ultra-stretch denim.",
                    Description = "The ultimate waist-defining fit. Flattering high rise combined with Levi's Stellar Stretch denim that sculpts and holds all day without bagging out.",
                    BasePrice = 4299m,
                    DiscountPrice = 2899m,
                    StockQuantity = 50,
                    LowStockThreshold = 10,
                    IsFeatured = true,
                    IsActive = true,
                    AverageRating = 4.7,
                    TotalReviews = 39,
                    CategoryId = catWomensFashion.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1541099649105-f69ad21f3246?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Karagiri Handwoven Banarasi Zari Soft Silk Saree",
                    Slug = "karagiri-handwoven-banarasi-silk-saree",
                    SKU = "KRG-BNR-RED",
                    ShortDescription = "Traditional crimson red pure silk Banarasi saree woven with golden zari floral motifs.",
                    Description = "A masterpiece of heritage craftsmanship. Woven in Varanasi with luminous mulberry silk and intricate gold zari brocade. Includes unstitched matching blouse piece.",
                    BasePrice = 11999m,
                    DiscountPrice = 6999m,
                    StockQuantity = 25,
                    LowStockThreshold = 4,
                    IsFeatured = true,
                    IsActive = true,
                    AverageRating = 4.9,
                    TotalReviews = 64,
                    CategoryId = catWomensFashion.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1617627143750-d86bc21e42bb?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Zara Double-Breasted Tailored Office Blazer",
                    Slug = "zara-double-breasted-tailored-blazer",
                    SKU = "ZAR-WBLZ-BEG",
                    ShortDescription = "Chic structured double-breasted beige blazer with horn buttons and peak lapels.",
                    Description = "Power dressing redefined. Features sharp padded shoulders, structured silhouette, interior silky lining, and flap pockets for an impeccable corporate or smart-casual look.",
                    BasePrice = 8490m,
                    DiscountPrice = 5990m,
                    StockQuantity = 30,
                    LowStockThreshold = 6,
                    IsFeatured = false,
                    IsActive = true,
                    AverageRating = 4.7,
                    TotalReviews = 29,
                    CategoryId = catWomensFashion.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1539109136881-3be0616acf4b?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "ONLY Casual Chiffon Puff Sleeve Top",
                    Slug = "only-casual-chiffon-puff-sleeve-top",
                    SKU = "ONL-TOP-WHT",
                    ShortDescription = "Lightweight pleated top with sweetheart neckline and romantic sheer puff sleeves.",
                    Description = "A charming day-to-night piece. Crafted from textured chiffon with smocked back panel for a flexible and flattering fit.",
                    BasePrice = 2499m,
                    DiscountPrice = 1499m,
                    StockQuantity = 45,
                    LowStockThreshold = 8,
                    IsFeatured = false,
                    IsActive = true,
                    AverageRating = 4.6,
                    TotalReviews = 23,
                    CategoryId = catWomensFashion.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Forever 21 Ribbed Knit Bodycon Midi Party Dress",
                    Slug = "forever-21-ribbed-knit-bodycon-midi-dress",
                    SKU = "F21-BDC-EMR",
                    ShortDescription = "Emerald green ribbed knit bodycon dress with side slit and square neckline.",
                    Description = "Sleek and flattering midi dress in premium stretch ribbed knit. Features sleeveless cut, side thigh-high slit, and contouring silhouette.",
                    BasePrice = 3299m,
                    DiscountPrice = 1999m,
                    StockQuantity = 40,
                    LowStockThreshold = 8,
                    IsFeatured = false,
                    IsActive = true,
                    AverageRating = 4.5,
                    TotalReviews = 34,
                    CategoryId = catWomensFashion.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1515372039744-b8f02a3ae446?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "H&M Oversized Knit Cardigan Sweater",
                    Slug = "hm-oversized-knit-cardigan-sweater",
                    SKU = "HM-CRD-LAV",
                    ShortDescription = "Cozy lavender chunky knit oversized cardigan with tortoiseshell buttons.",
                    Description = "Soft yarn cardigan in an oversized silhouette with V-neck, dropped shoulders, rib-knit trims, and wide sleeves for relaxed layered styling.",
                    BasePrice = 2999m,
                    DiscountPrice = 1899m,
                    StockQuantity = 35,
                    LowStockThreshold = 7,
                    IsFeatured = false,
                    IsActive = true,
                    AverageRating = 4.6,
                    TotalReviews = 18,
                    CategoryId = catWomensFashion.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1434389677669-e08b4cac3105?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Mango Classic Denim Trucker Jacket",
                    Slug = "mango-classic-denim-trucker-jacket",
                    SKU = "MNG-DNM-BLU",
                    ShortDescription = "Vintage wash 100% cotton denim trucker jacket with chest flap pockets.",
                    Description = "An evergreen layering essential. Medium wash denim jacket with silver metal buttons, pointed collar, and relaxed silhouette.",
                    BasePrice = 4990m,
                    DiscountPrice = 3290m,
                    StockQuantity = 30,
                    LowStockThreshold = 6,
                    IsFeatured = true,
                    IsActive = true,
                    AverageRating = 4.7,
                    TotalReviews = 26,
                    CategoryId = catWomensFashion.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1483985988355-763728e1935b?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "FabIndia Pure Cotton Lucknowi Chikankari Kurta",
                    Slug = "fabindia-cotton-lucknowi-chikankari-kurta",
                    SKU = "FAB-CHK-PCH",
                    ShortDescription = "Pastel peach breathable cotton straight kurta with handmade Lucknowi embroidery.",
                    Description = "Artisanal craftsmanship on pure breathable cambric cotton. Decorated with authentic tonal Chikankari shadow work and fine floral motifs.",
                    BasePrice = 3590m,
                    DiscountPrice = 2490m,
                    StockQuantity = 50,
                    LowStockThreshold = 10,
                    IsFeatured = false,
                    IsActive = true,
                    AverageRating = 4.8,
                    TotalReviews = 41,
                    CategoryId = catWomensFashion.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1583391733956-3750e0ff4e8b?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },

                // ==========================================
                // CATEGORY 4: HOME & KITCHEN (10 Products)
                // ==========================================
                new()
                {
                    Name = "Philips Premium Digital Airfryer XXL 1.4kg",
                    Slug = "philips-premium-digital-airfryer-xxl",
                    SKU = "PHL-AF-XXL",
                    ShortDescription = "Fat Removal technology with Twin TurboStar for crispy, healthy frying.",
                    Description = "The Philips Airfryer XXL uses hot air to fry your favorite food with little or no added oil. New Twin TurboStar technology removes excess fat.",
                    BasePrice = 18999m,
                    DiscountPrice = 14499m,
                    StockQuantity = 12,
                    LowStockThreshold = 2,
                    IsFeatured = true,
                    IsActive = true,
                    AverageRating = 4.7,
                    TotalReviews = 31,
                    CategoryId = catHome.Id,
                    BrandId = brandPhilips?.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1583394838336-acd977736f90?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Nordic Solid Oak Minimalist Coffee Table",
                    Slug = "nordic-solid-oak-minimalist-coffee-table",
                    SKU = "HOM-TBL-OAK",
                    ShortDescription = "Handcrafted natural oak wood table with rounded curves and matte finish.",
                    Description = "Bring organic elegance and Scandinavian charm to your living room with this solid oak table. High-grade natural timber with moisture-resistant coating.",
                    BasePrice = 12999m,
                    DiscountPrice = 8999m,
                    StockQuantity = 8,
                    LowStockThreshold = 2,
                    IsFeatured = false,
                    IsActive = true,
                    AverageRating = 4.8,
                    TotalReviews = 11,
                    CategoryId = catHome.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1533090161767-e6ffed986c88?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Instant Pot Duo 7-in-1 Electric Pressure Cooker 6L",
                    Slug = "instant-pot-duo-7-in-1-electric-cooker",
                    SKU = "HOM-INST-6L",
                    ShortDescription = "Pressure cooker, slow cooker, rice cooker, steamer, sauté pan, yogurt maker and warmer.",
                    Description = "Replaces 7 kitchen appliances. 13 customizable Smart Programs for one-touch cooking of soups, beans, rice, poultry, yogurt, and desserts.",
                    BasePrice = 11999m,
                    DiscountPrice = 8499m,
                    StockQuantity = 20,
                    LowStockThreshold = 4,
                    IsFeatured = true,
                    IsActive = true,
                    AverageRating = 4.8,
                    TotalReviews = 49,
                    CategoryId = catHome.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1544233726-9f1d2b27be8b?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Dyson V12 Detect Slim Cordless Vacuum",
                    Slug = "dyson-v12-detect-slim-cordless-vacuum",
                    SKU = "DYS-V12-SLM",
                    ShortDescription = "Laser illuminates invisible dust on hard floors with piezo sensor particle counting.",
                    Description = "Dyson's lightest intelligent cordless vacuum with laser illumination. Engineered for whole-home deep cleaning with up to 60 minutes of run time.",
                    BasePrice = 55900m,
                    DiscountPrice = 47900m,
                    StockQuantity = 10,
                    LowStockThreshold = 2,
                    IsFeatured = true,
                    IsActive = true,
                    AverageRating = 4.9,
                    TotalReviews = 27,
                    CategoryId = catHome.Id,
                    BrandId = brandDyson?.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1527443224154-c4a3942d3acf?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Nespresso Vertuo Pop Espresso Machine",
                    Slug = "nespresso-vertuo-pop-espresso-machine",
                    SKU = "NES-VRTO-POP",
                    ShortDescription = "Centrifusion extraction technology with one-touch brewing for 4 cup sizes.",
                    Description = "Adds a burst of color to your coffee lifestyle. Reads the barcode on each capsule to automatically adjust parameters for the perfect crema.",
                    BasePrice = 16999m,
                    DiscountPrice = 13999m,
                    StockQuantity = 15,
                    LowStockThreshold = 3,
                    IsFeatured = false,
                    IsActive = true,
                    AverageRating = 4.7,
                    TotalReviews = 19,
                    CategoryId = catHome.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1517668808822-9ebb02f2a0e6?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Prestige Iris 750W Mixer Grinder with 3 Stainless Jars",
                    Slug = "prestige-iris-750w-mixer-grinder",
                    SKU = "PRS-MG-750W",
                    ShortDescription = "750W copper motor with 3 stainless steel jars and transparent juicer jar.",
                    Description = "Designed to handle tough grinding tasks with ease. Features overload protection, sturdy ergonomically designed handles, and multi-function blade system.",
                    BasePrice = 4999m,
                    DiscountPrice = 3299m,
                    StockQuantity = 35,
                    LowStockThreshold = 6,
                    IsFeatured = false,
                    IsActive = true,
                    AverageRating = 4.5,
                    TotalReviews = 41,
                    CategoryId = catHome.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1556911220-e15b29be8c8f?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Solid Sheesham Wood 5-Shelf Bookcase",
                    Slug = "solid-sheesham-wood-5-shelf-bookcase",
                    SKU = "HOM-SHL-WOD",
                    ShortDescription = "Handcrafted Indian rosewood open bookshelf with walnut honey finish.",
                    Description = "Built from premium seasoned sheesham wood for exceptional strength and natural grain beauty. Ideal for books, plants, and home decor items.",
                    BasePrice = 16999m,
                    DiscountPrice = 11999m,
                    StockQuantity = 9,
                    LowStockThreshold = 2,
                    IsFeatured = false,
                    IsActive = true,
                    AverageRating = 4.6,
                    TotalReviews = 14,
                    CategoryId = catHome.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1594980596870-8aa52a78d8cd?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Milton Thermosteel Flip Lid Vacuum Flask 1000ml",
                    Slug = "milton-thermosteel-vacuum-flask-1000ml",
                    SKU = "MLT-THM-1000",
                    ShortDescription = "Double walled 304 stainless steel flask keeping beverages hot/cold for 24 hours.",
                    Description = "100% leak proof stainless steel insulated flask with convenient flip lid. Copper coating inside ensures superior temperature retention.",
                    BasePrice = 1299m,
                    DiscountPrice = 949m,
                    StockQuantity = 80,
                    LowStockThreshold = 15,
                    IsFeatured = false,
                    IsActive = true,
                    AverageRating = 4.7,
                    TotalReviews = 63,
                    CategoryId = catHome.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1602143407151-7111542de6e8?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Morphy Richards 30L Convection Microwave Oven",
                    Slug = "morphy-richards-30l-convection-microwave",
                    SKU = "MRP-MWO-30L",
                    ShortDescription = "Baking, grilling, defrosting with 200 auto cook menus and stainless steel cavity.",
                    Description = "Spacious 30L cavity allows multiple dish preparation simultaneously. Features motorized rotisserie, multistage cooking, and child safety lock.",
                    BasePrice = 17495m,
                    DiscountPrice = 12990m,
                    StockQuantity = 14,
                    LowStockThreshold = 3,
                    IsFeatured = false,
                    IsActive = true,
                    AverageRating = 4.5,
                    TotalReviews = 26,
                    CategoryId = catHome.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1574269909862-7e1d70bb8078?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Sleepwell Ortho Memory Foam King Size Mattress",
                    Slug = "sleepwell-ortho-memory-foam-king-mattress",
                    SKU = "SLP-MAT-KING",
                    ShortDescription = "Multi-layered spinal support mattress with breathable knitted fabric cover.",
                    Description = "Engineered for optimal spine alignment and pressure point relief. High-density base foam combined with temperature-regulating memory foam.",
                    BasePrice = 28999m,
                    DiscountPrice = 21499m,
                    StockQuantity = 7,
                    LowStockThreshold = 2,
                    IsFeatured = false,
                    IsActive = true,
                    AverageRating = 4.8,
                    TotalReviews = 18,
                    CategoryId = catHome.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1505693416388-ac5ce068fe85?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },

                // ==========================================
                // CATEGORY 4: BEAUTY & PERSONAL CARE (10 Products)
                // ==========================================
                new()
                {
                    Name = "Botanical Glow Vitamin C & Hyaluronic Acid Serum Duo",
                    Slug = "botanical-glow-vitamin-c-hyaluronic-serum",
                    SKU = "BTY-SRM-DUO",
                    ShortDescription = "Radiance boosting and deep hydration organic face serum set.",
                    Description = "Formulated with 15% pure Vitamin C, Ferulic Acid, and triple molecular weight Hyaluronic Acid to brighten complexion and reduce dark spots.",
                    BasePrice = 2499m,
                    DiscountPrice = 1799m,
                    StockQuantity = 60,
                    LowStockThreshold = 10,
                    IsFeatured = true,
                    IsActive = true,
                    AverageRating = 4.9,
                    TotalReviews = 56,
                    CategoryId = catBeauty.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Philips Multi-Grooming All-in-One 13-Piece Trimmer Set",
                    Slug = "philips-all-in-one-13-piece-trimmer-set",
                    SKU = "PHL-TRM-MG7715",
                    ShortDescription = "Self-sharpening DualCut blades, waterproof metal trimmer with 120 mins runtime.",
                    Description = "Ultimate styling for face, hair and body with 13 quality tools. DualCut technology includes 2x more blades that sharpen themselves as you work.",
                    BasePrice = 4995m,
                    DiscountPrice = 3495m,
                    StockQuantity = 45,
                    LowStockThreshold = 8,
                    IsFeatured = true,
                    IsActive = true,
                    AverageRating = 4.8,
                    TotalReviews = 72,
                    CategoryId = catBeauty.Id,
                    BrandId = brandPhilips?.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1503951914875-452162b0f3f1?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Dior Sauvage Eau De Parfum for Men (100ml)",
                    Slug = "dior-sauvage-eau-de-parfum-100ml",
                    SKU = "DIO-SVG-100ML",
                    ShortDescription = "Fresh spicy fragrance with Calabrian bergamot and Papua New Guinean vanilla.",
                    Description = "A powerfully fresh trail, Sauvage Eau de Parfum unfurls a noble and powerful composition with citrus facets and smoky accents.",
                    BasePrice = 14500m,
                    DiscountPrice = 12900m,
                    StockQuantity = 25,
                    LowStockThreshold = 5,
                    IsFeatured = true,
                    IsActive = true,
                    AverageRating = 4.9,
                    TotalReviews = 84,
                    CategoryId = catBeauty.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1523293182086-7651a899d37f?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "The Ordinary Niacinamide 10% + Zinc 1% (60ml)",
                    Slug = "the-ordinary-niacinamide-10-zinc-1",
                    SKU = "ORD-NC-60ML",
                    ShortDescription = "High-strength vitamin and mineral blemish formula to balance sebum activity.",
                    Description = "Targets breakouts, minimizes pores and clarifies congestion by boosting skin immunity and improving moisture retention.",
                    BasePrice = 1150m,
                    DiscountPrice = 950m,
                    StockQuantity = 75,
                    LowStockThreshold = 15,
                    IsFeatured = false,
                    IsActive = true,
                    AverageRating = 4.7,
                    TotalReviews = 91,
                    CategoryId = catBeauty.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1608248597359-05f32a51f496?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Dyson Supersonic Hair Dryer (Iron/Fuchsia)",
                    Slug = "dyson-supersonic-hair-dryer-iron-fuchsia",
                    SKU = "DYS-SUPR-HD",
                    ShortDescription = "Fast drying with intelligent heat control to protect natural hair shine.",
                    Description = "Small, powerful digital motor V9 combined with Air Multiplier technology produces high-velocity jet of controlled air for fast drying and precision styling.",
                    BasePrice = 38900m,
                    DiscountPrice = 32900m,
                    StockQuantity = 15,
                    LowStockThreshold = 3,
                    IsFeatured = true,
                    IsActive = true,
                    AverageRating = 4.9,
                    TotalReviews = 47,
                    CategoryId = catBeauty.Id,
                    BrandId = brandDyson?.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Cetaphil Gentle Skin Cleanser for Sensitive Skin (500ml)",
                    Slug = "cetaphil-gentle-skin-cleanser-500ml",
                    SKU = "CET-CLN-500",
                    ShortDescription = "Dermatologist recommended non-foaming hydrating formula with Niacinamide & Panthenol.",
                    Description = "Clinically proven to provide continuous hydration against dryness. Defends against 5 signs of skin sensitivity including weakened skin barrier and irritation.",
                    BasePrice = 999m,
                    DiscountPrice = 799m,
                    StockQuantity = 90,
                    LowStockThreshold = 20,
                    IsFeatured = false,
                    IsActive = true,
                    AverageRating = 4.8,
                    TotalReviews = 68,
                    CategoryId = catBeauty.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1556228720-195a672e8a03?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Neutrogena Hydro Boost Water Gel Moisturizer (50g)",
                    Slug = "neutrogena-hydro-boost-water-gel",
                    SKU = "NTG-HB-50G",
                    ShortDescription = "Unique light-weight gel with Hyaluronic Acid for 72-hour continuous hydration.",
                    Description = "Absorbs quickly like a gel, but provides intense long-lasting moisturizing power of a cream. Non-comedogenic and oil-free formula.",
                    BasePrice = 1150m,
                    DiscountPrice = 899m,
                    StockQuantity = 65,
                    LowStockThreshold = 12,
                    IsFeatured = false,
                    IsActive = true,
                    AverageRating = 4.7,
                    TotalReviews = 53,
                    CategoryId = catBeauty.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1598440947619-2c35fc9aa908?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "L'Oreal Paris Revitalift 1.5% Hyaluronic Acid Serum",
                    Slug = "loreal-revitalift-hyaluronic-acid-serum",
                    SKU = "LOR-RVL-30ML",
                    ShortDescription = "Intensive hydrating serum with 1.5% Hyaluronic Acid for instant plumping.",
                    Description = "Validated by dermatologists, this lightweight serum absorbs rapidly with no tacky residue to reduce fine lines and provide glowing radiant skin.",
                    BasePrice = 1099m,
                    DiscountPrice = 799m,
                    StockQuantity = 50,
                    LowStockThreshold = 10,
                    IsFeatured = false,
                    IsActive = true,
                    AverageRating = 4.6,
                    TotalReviews = 42,
                    CategoryId = catBeauty.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1601049541289-9b1b7bbbfe19?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Forest Essentials Luxury Ayurvedic Bath & Body Care Gift Set",
                    Slug = "forest-essentials-luxury-body-care-set",
                    SKU = "FE-LUX-GIFT",
                    ShortDescription = "Infused with pure honey, rosewater and cold-pressed organic sweet almond oil.",
                    Description = "Indulge in authentic Ayurvedic body care rituals. Contains shower butter, silkening body wash, handmade silk soap, and shimmering body mist.",
                    BasePrice = 4250m,
                    DiscountPrice = 3650m,
                    StockQuantity = 20,
                    LowStockThreshold = 4,
                    IsFeatured = false,
                    IsActive = true,
                    AverageRating = 4.8,
                    TotalReviews = 25,
                    CategoryId = catBeauty.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1522335789203-aabd1fc54bc9?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Maybelline New York Lash Sensational Sky High Mascara",
                    Slug = "maybelline-lash-sensational-sky-high-mascara",
                    SKU = "MAY-SKY-BLK",
                    ShortDescription = "Full volume and limitless length impact from every angle with Flex Tower brush.",
                    Description = "Infused with bamboo extract and fibers for long, full lashes that never get weighed down. Waterproof and allergy tested.",
                    BasePrice = 799m,
                    DiscountPrice = 599m,
                    StockQuantity = 70,
                    LowStockThreshold = 15,
                    IsFeatured = false,
                    IsActive = true,
                    AverageRating = 4.6,
                    TotalReviews = 38,
                    CategoryId = catBeauty.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1512496015851-a90fb38ba796?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },

                // ==========================================
                // CATEGORY 5: SPORTS & FITNESS (10 Products)
                // ==========================================
                new()
                {
                    Name = "Hex Rubber Dumbbell Set (5kg to 20kg) with 3-Tier Rack",
                    Slug = "hex-rubber-dumbbell-set-with-rack",
                    SKU = "SPT-HEX-SET",
                    ShortDescription = "Commercial grade anti-roll hex dumbbells with ergonomic knurled steel grips.",
                    Description = "Complete your home gym with heavy-duty hex rubber dumbbells designed for maximum durability, floor protection, and smooth ergonomic gripping.",
                    BasePrice = 21999m,
                    DiscountPrice = 16999m,
                    StockQuantity = 6,
                    LowStockThreshold = 2,
                    IsFeatured = true,
                    IsActive = true,
                    AverageRating = 4.8,
                    TotalReviews = 14,
                    CategoryId = catSports.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1586401100295-7a8096fd231a?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Nike Dri-FIT Men's Training Legend Tee",
                    Slug = "nike-dri-fit-mens-training-legend-tee",
                    SKU = "NKE-DFT-BLK",
                    ShortDescription = "Sweat-wicking Dri-FIT fabric with odor-resistant finish for intense workouts.",
                    Description = "The Nike Dri-FIT Legend T-Shirt is made from lightweight, sweat-wicking fabric with an anti-odor finish to help keep you dry and comfortable from warm-ups to cool-downs.",
                    BasePrice = 1795m,
                    DiscountPrice = 1295m,
                    StockQuantity = 60,
                    LowStockThreshold = 12,
                    IsFeatured = false,
                    IsActive = true,
                    AverageRating = 4.7,
                    TotalReviews = 37,
                    CategoryId = catSports.Id,
                    BrandId = brandNike?.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1581655353564-df123a1eb820?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Garmin Forerunner 265 GPS Running Smartwatch",
                    Slug = "garmin-forerunner-265-gps-smartwatch",
                    SKU = "GRM-FR265-AQUA",
                    ShortDescription = "Brilliant AMOLED touchscreen display with advanced training metrics and recovery insights.",
                    Description = "Plan your race strategy with personalized daily suggested workouts, training readiness scores, multi-band GNSS GPS and up to 13 days of battery life.",
                    BasePrice = 46990m,
                    DiscountPrice = 41990m,
                    StockQuantity = 12,
                    LowStockThreshold = 3,
                    IsFeatured = true,
                    IsActive = true,
                    AverageRating = 4.9,
                    TotalReviews = 28,
                    CategoryId = catSports.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1508685096489-7aacd43bd3b1?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Optimum Nutrition Gold Standard 100% Whey Protein (2kg)",
                    Slug = "optimum-nutrition-gold-standard-whey-2kg",
                    SKU = "ON-WHEY-2KG-DBL",
                    ShortDescription = "24g protein, 5.5g BCAAs and 4g glutamine per serving in Double Rich Chocolate.",
                    Description = "The world's bestselling whey protein powder. Whey protein isolates are the primary ingredient with fast digestion and optimal muscle recovery.",
                    BasePrice = 7899m,
                    DiscountPrice = 6499m,
                    StockQuantity = 40,
                    LowStockThreshold = 8,
                    IsFeatured = true,
                    IsActive = true,
                    AverageRating = 4.9,
                    TotalReviews = 112,
                    CategoryId = catSports.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1579722821273-0f6c7d44362f?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Boldfit Eco-Friendly Anti-Skid Yoga Mat 6mm",
                    Slug = "boldfit-eco-friendly-anti-skid-yoga-mat",
                    SKU = "BLD-YGA-MAT-6MM",
                    ShortDescription = "TPE dual-layer non-slip exercise mat with alignment guide lines and carry strap.",
                    Description = "High-density cushioned TPE material provides optimal joint protection. Moisture-resistant surface cleans easily with soap and water.",
                    BasePrice = 1999m,
                    DiscountPrice = 1299m,
                    StockQuantity = 50,
                    LowStockThreshold = 10,
                    IsFeatured = false,
                    IsActive = true,
                    AverageRating = 4.6,
                    TotalReviews = 45,
                    CategoryId = catSports.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1592432678016-e910b452f9a2?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Yonex Muscle Power 29 Light Badminton Racquet",
                    Slug = "yonex-muscle-power-29-light-badminton-racquet",
                    SKU = "YNX-MP29-LT",
                    ShortDescription = "High modulus graphite frame, isometric head shape, 85g light-weight with full cover.",
                    Description = "Muscle Power locates the string on rounded archways that eliminate stress-load and fatigue through contact friction for powerful, explosive smashes.",
                    BasePrice = 3890m,
                    DiscountPrice = 2790m,
                    StockQuantity = 30,
                    LowStockThreshold = 6,
                    IsFeatured = false,
                    IsActive = true,
                    AverageRating = 4.7,
                    TotalReviews = 39,
                    CategoryId = catSports.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1626224583764-f87db24ac4ea?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Adidas Pro Indoor/Outdoor Basketball (Size 7)",
                    Slug = "adidas-pro-indoor-outdoor-basketball-size7",
                    SKU = "ADS-BB-PRO-SZ7",
                    ShortDescription = "Composite leather moisture-absorbing cover for superior grip in any weather.",
                    Description = "Built for hardwood courts and blacktop games alike. Deep channels give your fingers natural touch points for shooting consistency and ball handling.",
                    BasePrice = 2999m,
                    DiscountPrice = 1999m,
                    StockQuantity = 35,
                    LowStockThreshold = 7,
                    IsFeatured = false,
                    IsActive = true,
                    AverageRating = 4.6,
                    TotalReviews = 21,
                    CategoryId = catSports.Id,
                    BrandId = brandAdidas?.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1519861531473-9200262188bf?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Decathlon Domyos Foldable Multi-Position Weight Bench",
                    Slug = "decathlon-domyos-foldable-weight-bench",
                    SKU = "DEC-DMY-BNCH",
                    ShortDescription = "4 backrest inclines (-15°, 0°, 30°, 80°) with 220kg maximum weight capacity.",
                    Description = "Compact bench folds away in less than 30 seconds. Perfect for dumbbell presses, incline curls, and core abdominal workouts at home.",
                    BasePrice = 8999m,
                    DiscountPrice = 6999m,
                    StockQuantity = 15,
                    LowStockThreshold = 3,
                    IsFeatured = false,
                    IsActive = true,
                    AverageRating = 4.7,
                    TotalReviews = 19,
                    CategoryId = catSports.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1517838277536-f5f99be501cd?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Speedo Fastskin Hyper Elite Anti-Fog Swimming Goggles",
                    Slug = "speedo-fastskin-hyper-elite-swimming-goggles",
                    SKU = "SPD-HYPR-GOG",
                    ShortDescription = "Hydroscopic lens profile with IQfit 3D goggle seal for leak-free racing fit.",
                    Description = "Packed with hydrodynamic and performance enhancing technologies. Mirrored lenses reduce glare and brightness during competitive pool sessions.",
                    BasePrice = 4499m,
                    DiscountPrice = 3299m,
                    StockQuantity = 25,
                    LowStockThreshold = 5,
                    IsFeatured = false,
                    IsActive = true,
                    AverageRating = 4.8,
                    TotalReviews = 23,
                    CategoryId = catSports.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1530549387789-4c1017266635?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Nivia Storm Football Rubber Moulded (Size 5)",
                    Slug = "nivia-storm-football-rubber-moulded",
                    SKU = "NIV-STRM-SZ5",
                    ShortDescription = "32 panel rubber moulded construction suitable for hard and wet ground play.",
                    Description = "Durable rubber outer cover with air-retention butyl bladder. High abrasion resistance designed for rugged Indian playing conditions.",
                    BasePrice = 999m,
                    DiscountPrice = 699m,
                    StockQuantity = 60,
                    LowStockThreshold = 12,
                    IsFeatured = false,
                    IsActive = true,
                    AverageRating = 4.5,
                    TotalReviews = 40,
                    CategoryId = catSports.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1614632537423-1e6c2e7e0aab?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },

                // ==========================================
                // CATEGORY 6: BOOKS & STATIONERY (10 Products)
                // ==========================================
                new()
                {
                    Name = "Clean Architecture: A Craftsman's Guide to Software Structure",
                    Slug = "clean-architecture-craftsmans-guide",
                    SKU = "BK-CLEAN-ARCH",
                    ShortDescription = "By Robert C. Martin (Uncle Bob). Essential reading for software architects and developers.",
                    Description = "By applying universal rules of software architecture, you can dramatically improve developer productivity throughout the life of any software system.",
                    BasePrice = 1499m,
                    DiscountPrice = 999m,
                    StockQuantity = 35,
                    LowStockThreshold = 5,
                    IsFeatured = true,
                    IsActive = true,
                    AverageRating = 4.9,
                    TotalReviews = 78,
                    CategoryId = catBooks.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1532012164546-f432f2e3777a?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Atomic Habits: An Easy & Proven Way to Build Good Habits",
                    Slug = "atomic-habits-james-clear-hardcover",
                    SKU = "BK-ATMC-HABT",
                    ShortDescription = "By James Clear. #1 New York Times Bestseller with over 15 million copies sold.",
                    Description = "If you're having trouble changing your habits, the problem isn't you. The problem is your system. James Clear distills proven concepts into practical daily behaviors.",
                    BasePrice = 899m,
                    DiscountPrice = 599m,
                    StockQuantity = 80,
                    LowStockThreshold = 15,
                    IsFeatured = true,
                    IsActive = true,
                    AverageRating = 4.9,
                    TotalReviews = 142,
                    CategoryId = catBooks.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1544947950-fa07a98d237f?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "The Psychology of Money: Timeless Lessons on Wealth, Greed, and Happiness",
                    Slug = "the-psychology-of-money-morgan-housel",
                    SKU = "BK-PSY-MONEY",
                    ShortDescription = "By Morgan Housel. 19 short stories exploring the strange ways people think about money.",
                    Description = "Doing well with money isn't necessarily about what you know. It's about how you behave. And behavior is hard to teach, even to really smart people.",
                    BasePrice = 599m,
                    DiscountPrice = 399m,
                    StockQuantity = 90,
                    LowStockThreshold = 20,
                    IsFeatured = true,
                    IsActive = true,
                    AverageRating = 4.8,
                    TotalReviews = 119,
                    CategoryId = catBooks.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1592496431122-2349e0fbc666?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Designing Data-Intensive Applications",
                    Slug = "designing-data-intensive-applications",
                    SKU = "BK-DDIA-KLEP",
                    ShortDescription = "By Martin Kleppmann. The definitive guide to storage, processing, and distributed systems.",
                    Description = "Data is at the center of many challenges in system design today. Martin Kleppmann helps you navigate the diverse landscape of databases, messaging, and stream systems.",
                    BasePrice = 2499m,
                    DiscountPrice = 1899m,
                    StockQuantity = 30,
                    LowStockThreshold = 6,
                    IsFeatured = false,
                    IsActive = true,
                    AverageRating = 4.9,
                    TotalReviews = 58,
                    CategoryId = catBooks.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1512820790803-83ca734da794?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Parker Sonnet Lacquer Fountain Pen with Gold Trim",
                    Slug = "parker-sonnet-lacquer-fountain-pen-gold-trim",
                    SKU = "STN-PKR-SNNT",
                    ShortDescription = "Handcrafted stainless steel medium nib with glossy black lacquer and 23k gold finish.",
                    Description = "A timeless symbol of elegance. Hand assembled and checked for flawless quality, providing high precision and exceptional writing comfort.",
                    BasePrice = 8499m,
                    DiscountPrice = 6499m,
                    StockQuantity = 20,
                    LowStockThreshold = 4,
                    IsFeatured = true,
                    IsActive = true,
                    AverageRating = 4.8,
                    TotalReviews = 32,
                    CategoryId = catBooks.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1583485088034-697b5bc54ccd?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Moleskine Classic Hardcover Dotted Journal (A5, Black)",
                    Slug = "moleskine-classic-hardcover-dotted-journal-a5",
                    SKU = "STN-MLS-A5-DOT",
                    ShortDescription = "240 acid-free ivory pages with elastic closure band and expandable inner pocket.",
                    Description = "The legendary notebook used by artists and thinkers over the past two centuries. Rounded corners, ribbon bookmark and lie-flat 180° opening.",
                    BasePrice = 2299m,
                    DiscountPrice = 1799m,
                    StockQuantity = 45,
                    LowStockThreshold = 10,
                    IsFeatured = false,
                    IsActive = true,
                    AverageRating = 4.8,
                    TotalReviews = 49,
                    CategoryId = catBooks.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Staedtler Triplus Fineliner 0.3mm Pens (Set of 20 Colors)",
                    Slug = "staedtler-triplus-fineliner-03mm-set-of-20",
                    SKU = "STN-STD-20SET",
                    ShortDescription = "Ergonomic triangular barrel with superfine metal-clad tip and DRY SAFE ink.",
                    Description = "Fineliner with superfine, metal-clad tip. Ergonomic triangular shape for relaxed and easy writing. Can be left uncapped for days without drying up.",
                    BasePrice = 1999m,
                    DiscountPrice = 1499m,
                    StockQuantity = 40,
                    LowStockThreshold = 8,
                    IsFeatured = false,
                    IsActive = true,
                    AverageRating = 4.7,
                    TotalReviews = 36,
                    CategoryId = catBooks.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1585336261026-41ff36d65624?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Sapiens: A Brief History of Humankind",
                    Slug = "sapiens-brief-history-of-humankind",
                    SKU = "BK-SAP-YUVAL",
                    ShortDescription = "By Yuval Noah Harari. Groundbreaking narrative of humanity's creation and evolution.",
                    Description = "From a renowned historian comes a groundbreaking narrative of humanity’s creation and evolution that explores the ways in which biology and history have defined us.",
                    BasePrice = 699m,
                    DiscountPrice = 499m,
                    StockQuantity = 60,
                    LowStockThreshold = 12,
                    IsFeatured = false,
                    IsActive = true,
                    AverageRating = 4.8,
                    TotalReviews = 88,
                    CategoryId = catBooks.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1543002588-bfa74002ed7e?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Deep Work: Rules for Focused Success in a Distracted World",
                    Slug = "deep-work-cal-newport",
                    SKU = "BK-DEEP-WORK",
                    ShortDescription = "By Cal Newport. The indispensable guide to mastering high-value productivity skills.",
                    Description = "Deep work is the ability to focus without distraction on a cognitively demanding task. A skill that allows you to quickly master complicated information and produce better results.",
                    BasePrice = 599m,
                    DiscountPrice = 399m,
                    StockQuantity = 55,
                    LowStockThreshold = 10,
                    IsFeatured = false,
                    IsActive = true,
                    AverageRating = 4.7,
                    TotalReviews = 62,
                    CategoryId = catBooks.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1497633762265-9d179a990aa6?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Midori MD Minimalist Daily Journal Planner",
                    Slug = "midori-md-minimalist-daily-journal-planner",
                    SKU = "STN-MDR-JRNL",
                    ShortDescription = "Japanese bleed-resistant MD paper designed for fountain pens and artistic journaling.",
                    Description = "Crafted in Japan with utmost attention to writing comfort. MD Paper is specially engineered to prevent ink smearing and bleed-through for both fountain pens and pencils.",
                    BasePrice = 1899m,
                    DiscountPrice = 1450m,
                    StockQuantity = 30,
                    LowStockThreshold = 6,
                    IsFeatured = false,
                    IsActive = true,
                    AverageRating = 4.8,
                    TotalReviews = 29,
                    CategoryId = catBooks.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1516962215378-7fa2e137ae93?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                }
            };

            foreach (var prod in allProducts)
            {
                if (!await context.Products.AnyAsync(p => p.Slug == prod.Slug))
                {
                    await context.Products.AddAsync(prod);
                }
            }
            await context.SaveChangesAsync();

            // 7. Seed Sample Reviews
            var firstProduct = await context.Products.FirstOrDefaultAsync();
            if (customerUser != null && firstProduct != null && !await context.Reviews.AnyAsync())
            {
                var reviews = new List<Review>
                {
                    new()
                    {
                        ProductId = firstProduct.Id,
                        UserId = customerUser.Id,
                        Rating = 5,
                        Title = "Absolute powerhouse machine! Highly recommended.",
                        Comment = "The performance and battery life on this laptop is sensational. Handles 4K video editing, heavy Docker containers and compiles without breaking a sweat.",
                        IsVerifiedPurchase = true,
                        IsApproved = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-5)
                    }
                };
                await context.Reviews.AddRangeAsync(reviews);
                await context.SaveChangesAsync();
            }

            // 8. Seed Coupons
            if (!await context.Coupons.AnyAsync())
            {
                var coupons = new List<Coupon>
                {
                    new()
                    {
                        Code = "WELCOME10",
                        Description = "10% Instant discount on your first purchase",
                        DiscountType = DiscountType.Percentage,
                        DiscountValue = 10,
                        MinOrderAmount = 500,
                        MaxDiscountAmount = 500,
                        StartDate = DateTime.UtcNow.AddDays(-10),
                        ExpiryDate = DateTime.UtcNow.AddMonths(3),
                        UsageLimit = 1000,
                        IsActive = true
                    },
                    new()
                    {
                        Code = "FLAT500",
                        Description = "Flat ₹500 discount on orders above ₹2,999",
                        DiscountType = DiscountType.FixedAmount,
                        DiscountValue = 500,
                        MinOrderAmount = 2999,
                        StartDate = DateTime.UtcNow.AddDays(-10),
                        ExpiryDate = DateTime.UtcNow.AddMonths(2),
                        UsageLimit = 500,
                        IsActive = true
                    },
                    new()
                    {
                        Code = "SUPERDEAL",
                        Description = "20% Super Festive Discount up to ₹1,500",
                        DiscountType = DiscountType.Percentage,
                        DiscountValue = 20,
                        MinOrderAmount = 1999,
                        MaxDiscountAmount = 1500,
                        StartDate = DateTime.UtcNow.AddDays(-5),
                        ExpiryDate = DateTime.UtcNow.AddMonths(1),
                        UsageLimit = 200,
                        IsActive = true
                    }
                };
                await context.Coupons.AddRangeAsync(coupons);
                await context.SaveChangesAsync();
            }

            // 9. Seed Sample Orders if none exist
            if (!await context.Orders.AnyAsync() && customerUser != null)
            {
                var prod1 = await context.Products.FirstOrDefaultAsync();
                if (prod1 != null)
                {
                    var sampleOrder = new Order
                    {
                        OrderNumber = "ORD-20260901-1001",
                        UserId = customerUser.Id,
                        OrderDate = DateTime.UtcNow.AddDays(-3),
                        SubTotal = prod1.DiscountPrice ?? prod1.BasePrice,
                        DiscountAmount = 500,
                        ShippingFee = 0,
                        TaxAmount = Math.Round(((prod1.DiscountPrice ?? prod1.BasePrice) - 500) * 0.18m, 2),
                        GrandTotal = ((prod1.DiscountPrice ?? prod1.BasePrice) - 500) + Math.Round(((prod1.DiscountPrice ?? prod1.BasePrice) - 500) * 0.18m, 2),
                        Status = OrderStatus.Delivered,
                        PaymentStatus = PaymentStatus.Paid,
                        PaymentMethod = PaymentMethod.CreditOrDebitCard,
                        CouponCode = "FLAT500",
                        ShippingFullName = "Anand Mishra",
                        ShippingPhone = "+91 9123456780",
                        ShippingAddressLine1 = "Flat 402, Sunshine Heights, MG Road",
                        ShippingCity = "Noida",
                        ShippingState = "Uttar Pradesh",
                        ShippingPostalCode = "201301",
                        ShippingCountry = "India",
                        CreatedAt = DateTime.UtcNow.AddDays(-3)
                    };

                    sampleOrder.OrderItems.Add(new OrderItem
                    {
                        ProductId = prod1.Id,
                        ProductName = prod1.Name,
                        SKU = prod1.SKU,
                        ProductImageUrl = prod1.Images.FirstOrDefault()?.ImageUrl,
                        UnitPrice = prod1.DiscountPrice ?? prod1.BasePrice,
                        Quantity = 1,
                        TotalPrice = prod1.DiscountPrice ?? prod1.BasePrice,
                        CreatedAt = DateTime.UtcNow.AddDays(-3)
                    });

                    sampleOrder.StatusHistories.Add(new OrderStatusHistory
                    {
                        Status = OrderStatus.Confirmed,
                        Notes = "Order placed and paid online.",
                        ChangedBy = "Customer",
                        CreatedAt = DateTime.UtcNow.AddDays(-3)
                    });
                    sampleOrder.StatusHistories.Add(new OrderStatusHistory
                    {
                        Status = OrderStatus.Shipped,
                        Notes = "Package dispatched via BlueDart Express (Tracking: BDT98765432).",
                        ChangedBy = "Admin",
                        CreatedAt = DateTime.UtcNow.AddDays(-2)
                    });
                    sampleOrder.StatusHistories.Add(new OrderStatusHistory
                    {
                        Status = OrderStatus.Delivered,
                        Notes = "Delivered to Anand Mishra.",
                        ChangedBy = "Courier",
                        CreatedAt = DateTime.UtcNow.AddDays(-1)
                    });

                    await context.Orders.AddAsync(sampleOrder);
                    await context.SaveChangesAsync();
                }
            }
        }

        // 7. Seed Additional Men's Fashion Products incrementally
        await SeedAdditionalMensFashionProductsAsync(context);

        // 8. Ensure ALL clothing & footwear products have complete Size options
        await EnsureAllClothingHasSizesAsync(context);
    }

        private static async Task SeedAdditionalMensFashionProductsAsync(ApplicationDbContext context)
        {
            var catMensFashion = await context.Categories.FirstOrDefaultAsync(c => c.Slug == "mens-fashion");
            if (catMensFashion == null) return;

            var brandNike = await context.Brands.FirstOrDefaultAsync(b => b.Slug == "nike");
            var brandAdidas = await context.Brands.FirstOrDefaultAsync(b => b.Slug == "adidas");
            var brandPuma = await context.Brands.FirstOrDefaultAsync(b => b.Slug == "puma");

            var newMensProducts = new List<Product>
            {
                new()
                {
                    Name = "Allen Solly Men's Slim Fit Formal Cotton Trousers",
                    Slug = "allen-solly-slim-fit-formal-trousers",
                    SKU = "AS-TRS-CHR",
                    ShortDescription = "Wrinkle-resistant flat-front formal trousers crafted in premium stretch cotton.",
                    Description = "Upgrade your corporate and evening wardrobe with Allen Solly's tailored flat-front formal trousers. Featuring a mid-rise waist, slash pockets, and flexible comfort waistband.",
                    BasePrice = 2999m,
                    DiscountPrice = 1899m,
                    StockQuantity = 45,
                    LowStockThreshold = 8,
                    IsFeatured = true,
                    IsActive = true,
                    AverageRating = 4.7,
                    TotalReviews = 36,
                    CategoryId = catMensFashion.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1624378439575-d8705ad7ae80?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    },
                    Variants = new List<ProductVariant>
                    {
                        new() { VariantName = "30 / Charcoal Grey", SKU = "AS-TRS-30", Price = 1899m, StockQuantity = 10 },
                        new() { VariantName = "32 / Charcoal Grey", SKU = "AS-TRS-32", Price = 1899m, StockQuantity = 15 },
                        new() { VariantName = "34 / Charcoal Grey", SKU = "AS-TRS-34", Price = 1899m, StockQuantity = 12 },
                        new() { VariantName = "36 / Charcoal Grey", SKU = "AS-TRS-36", Price = 1899m, StockQuantity = 8 }
                    }
                },
                new()
                {
                    Name = "Peter England Royal Blue Textured Two-Piece Formal Suit",
                    Slug = "peter-england-royal-blue-two-piece-formal-suit",
                    SKU = "PE-SUIT-RBL",
                    ShortDescription = "Modern slim-fit 2-piece notch lapel blazer and formal trousers set in royal navy blue.",
                    Description = "Designed for weddings, receptions, and boardroom presentations. Crafted from premium wrinkle-resistant poly-viscose blend with satin inner lining.",
                    BasePrice = 12999m,
                    DiscountPrice = 8499m,
                    StockQuantity = 20,
                    LowStockThreshold = 4,
                    IsFeatured = true,
                    IsActive = true,
                    AverageRating = 4.9,
                    TotalReviews = 28,
                    CategoryId = catMensFashion.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1594938298603-c8148c4dae35?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    },
                    Variants = new List<ProductVariant>
                    {
                        new() { VariantName = "38 / Royal Blue", SKU = "PE-SUIT-38", Price = 8499m, StockQuantity = 6 },
                        new() { VariantName = "40 / Royal Blue", SKU = "PE-SUIT-40", Price = 8499m, StockQuantity = 8 },
                        new() { VariantName = "42 / Royal Blue", SKU = "PE-SUIT-42", Price = 8499m, StockQuantity = 6 }
                    }
                },
                new()
                {
                    Name = "Nike Dri-FIT Legend Men's Fitness Training T-Shirt",
                    Slug = "nike-dri-fit-legend-training-tshirt",
                    SKU = "NKE-TSH-DRF",
                    ShortDescription = "Odor-resistant breathable workout tee powered by moisture-wicking Dri-FIT tech.",
                    Description = "The Nike Dri-FIT Legend T-Shirt is made with lightweight, sweat-wicking fabric with an anti-odor finish to help keep you dry and comfortable from warm-ups through cool-downs.",
                    BasePrice = 1995m,
                    DiscountPrice = 1395m,
                    StockQuantity = 65,
                    LowStockThreshold = 10,
                    IsFeatured = false,
                    IsActive = true,
                    AverageRating = 4.8,
                    TotalReviews = 52,
                    CategoryId = catMensFashion.Id,
                    BrandId = brandNike?.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1521572267360-ee0c2909d518?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    },
                    Variants = new List<ProductVariant>
                    {
                        new() { VariantName = "M / Matte Black", SKU = "NKE-DRF-M", Price = 1395m, StockQuantity = 25 },
                        new() { VariantName = "L / Matte Black", SKU = "NKE-DRF-L", Price = 1395m, StockQuantity = 25 },
                        new() { VariantName = "XL / Matte Black", SKU = "NKE-DRF-XL", Price = 1395m, StockQuantity = 15 }
                    }
                },
                new()
                {
                    Name = "Wrangler Rugged Wear Denim Sherpa Trucker Jacket",
                    Slug = "wrangler-rugged-denim-sherpa-jacket",
                    SKU = "WRG-JKT-SHP",
                    ShortDescription = "Heavyweight 100% cotton denim jacket with plush sherpa fleece lining.",
                    Description = "Built for warmth and rugged durability. Features authentic trucker silhouette, warm sherpa fleece lining on body and collar, chest flap pockets, and heavy-duty brass button closures.",
                    BasePrice = 6999m,
                    DiscountPrice = 4499m,
                    StockQuantity = 30,
                    LowStockThreshold = 5,
                    IsFeatured = true,
                    IsActive = true,
                    AverageRating = 4.8,
                    TotalReviews = 41,
                    CategoryId = catMensFashion.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1576995853123-5a10305d93c0?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    },
                    Variants = new List<ProductVariant>
                    {
                        new() { VariantName = "M / Vintage Stonewash", SKU = "WRG-SHP-M", Price = 4499m, StockQuantity = 10 },
                        new() { VariantName = "L / Vintage Stonewash", SKU = "WRG-SHP-L", Price = 4499m, StockQuantity = 12 },
                        new() { VariantName = "XL / Vintage Stonewash", SKU = "WRG-SHP-XL", Price = 4499m, StockQuantity = 8 }
                    }
                },
                new()
                {
                    Name = "FabIndia Pure Linen Handloom Nehru Jacket / Bundi",
                    Slug = "fabindia-pure-linen-handloom-nehru-jacket",
                    SKU = "FBI-NHR-MST",
                    ShortDescription = "Mandarin collar sleeveless ethnic waistcoat crafted in handwoven pure linen.",
                    Description = "Add ethnic sophistication to your kurtas and formal shirts with this textured handwoven linen Nehru jacket. Features a sleek mandarin collar, welt pockets, and brass button detailing.",
                    BasePrice = 4999m,
                    DiscountPrice = 3499m,
                    StockQuantity = 35,
                    LowStockThreshold = 6,
                    IsFeatured = false,
                    IsActive = true,
                    AverageRating = 4.6,
                    TotalReviews = 19,
                    CategoryId = catMensFashion.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1583743814966-8936f5b7be1a?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    },
                    Variants = new List<ProductVariant>
                    {
                        new() { VariantName = "38 / Mustard Ochre", SKU = "FBI-NHR-38", Price = 3499m, StockQuantity = 10 },
                        new() { VariantName = "40 / Mustard Ochre", SKU = "FBI-NHR-40", Price = 3499m, StockQuantity = 15 },
                        new() { VariantName = "42 / Mustard Ochre", SKU = "FBI-NHR-42", Price = 3499m, StockQuantity = 10 }
                    }
                },
                new()
                {
                    Name = "Louis Philippe Premium Handcrafted Italian Leather Oxford Shoes",
                    Slug = "louis-philippe-italian-leather-oxford-shoes",
                    SKU = "LP-SHOE-BRN",
                    ShortDescription = "Genuine full-grain leather lace-up formal oxford shoes with cushioned insole.",
                    Description = "Handcrafted by master artisans using Italian full-grain burnished leather. Features a closed lacing system, Goodyear welted sole, and memory foam footbed for executive comfort.",
                    BasePrice = 7999m,
                    DiscountPrice = 5199m,
                    StockQuantity = 28,
                    LowStockThreshold = 5,
                    IsFeatured = true,
                    IsActive = true,
                    AverageRating = 4.9,
                    TotalReviews = 34,
                    CategoryId = catMensFashion.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1614252235316-8c857d38b5f4?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    },
                    Variants = new List<ProductVariant>
                    {
                        new() { VariantName = "UK 7 / Tan Brown", SKU = "LP-SHOE-07", Price = 5199m, StockQuantity = 6 },
                        new() { VariantName = "UK 8 / Tan Brown", SKU = "LP-SHOE-08", Price = 5199m, StockQuantity = 8 },
                        new() { VariantName = "UK 9 / Tan Brown", SKU = "LP-SHOE-09", Price = 5199m, StockQuantity = 8 },
                        new() { VariantName = "UK 10 / Tan Brown", SKU = "LP-SHOE-10", Price = 5199m, StockQuantity = 6 }
                    }
                },
                new()
                {
                    Name = "Wildcraft Tech Waterproof Hooded Rain & Wind Cheater",
                    Slug = "wildcraft-waterproof-hooded-wind-cheater",
                    SKU = "WLD-WND-NVY",
                    ShortDescription = "Seam-sealed breathable waterproof jacket with adjustable hood and packable pouch.",
                    Description = "Engineered for daily monsoon commutes and mountain treks. 100% waterproof shell with high-density nylon taffeta, taped seams, and underarm ventilation zips.",
                    BasePrice = 3799m,
                    DiscountPrice = 2499m,
                    StockQuantity = 40,
                    LowStockThreshold = 8,
                    IsFeatured = false,
                    IsActive = true,
                    AverageRating = 4.6,
                    TotalReviews = 23,
                    CategoryId = catMensFashion.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1548883354-7622d03aca27?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    },
                    Variants = new List<ProductVariant>
                    {
                        new() { VariantName = "M / Navy Blue", SKU = "WLD-WND-M", Price = 2499m, StockQuantity = 15 },
                        new() { VariantName = "L / Navy Blue", SKU = "WLD-WND-L", Price = 2499m, StockQuantity = 15 },
                        new() { VariantName = "XL / Navy Blue", SKU = "WLD-WND-XL", Price = 2499m, StockQuantity = 10 }
                    }
                },
                new()
                {
                    Name = "Puma Smash V2 Leather Low-Top Men's Sneakers",
                    Slug = "puma-smash-v2-leather-low-top-sneakers",
                    SKU = "PUM-SNK-WHT",
                    ShortDescription = "Tennis-inspired classic white leather sneakers with SoftFoam+ comfort sockliner.",
                    Description = "The Puma Smash v2 is the new interpretation of the Puma Smash icon. The tennis-inspired silhouette features a soft leather upper with an improved fit and durable rubber outsole.",
                    BasePrice = 4499m,
                    DiscountPrice = 2699m,
                    StockQuantity = 50,
                    LowStockThreshold = 10,
                    IsFeatured = true,
                    IsActive = true,
                    AverageRating = 4.7,
                    TotalReviews = 47,
                    CategoryId = catMensFashion.Id,
                    BrandId = brandPuma?.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1608231387042-66d1773070a5?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    },
                    Variants = new List<ProductVariant>
                    {
                        new() { VariantName = "UK 7 / Classic White", SKU = "PUM-SNK-07", Price = 2699m, StockQuantity = 10 },
                        new() { VariantName = "UK 8 / Classic White", SKU = "PUM-SNK-08", Price = 2699m, StockQuantity = 15 },
                        new() { VariantName = "UK 9 / Classic White", SKU = "PUM-SNK-09", Price = 2699m, StockQuantity = 15 },
                        new() { VariantName = "UK 10 / Classic White", SKU = "PUM-SNK-10", Price = 2699m, StockQuantity = 10 }
                    }
                },
                new()
                {
                    Name = "Titan Karishma Analog Silver Dial Stainless Steel Watch for Men",
                    Slug = "titan-karishma-analog-stainless-steel-mens-watch",
                    SKU = "TTN-WTC-SLV",
                    ShortDescription = "Classic mineral glass quartz watch with stainless steel strap and date display.",
                    Description = "Elegance meets everyday durability. Powered by high-precision quartz movement, water-resistant up to 30 meters, with a sunray silver dial and polished metallic strap.",
                    BasePrice = 3995m,
                    DiscountPrice = 2795m,
                    StockQuantity = 30,
                    LowStockThreshold = 5,
                    IsFeatured = false,
                    IsActive = true,
                    AverageRating = 4.8,
                    TotalReviews = 38,
                    CategoryId = catMensFashion.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1524805444758-089113d48a6d?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    }
                },
                new()
                {
                    Name = "Raymond Luxury Merino Wool Notch Lapel Winter Overcoat",
                    Slug = "raymond-luxury-merino-wool-winter-overcoat",
                    SKU = "RYM-COAT-CAM",
                    ShortDescription = "Tailored 100% fine Merino wool full-length overcoat in elegant camel tan.",
                    Description = "The epitome of gentlemanly luxury for cold weather. Crafted from rich Australian Merino wool with horn buttons, center back vent, and insulating quilted lining.",
                    BasePrice = 15999m,
                    DiscountPrice = 10999m,
                    StockQuantity = 15,
                    LowStockThreshold = 3,
                    IsFeatured = true,
                    IsActive = true,
                    AverageRating = 4.9,
                    TotalReviews = 25,
                    CategoryId = catMensFashion.Id,
                    Images = new List<ProductImage>
                    {
                        new() { ImageUrl = "https://images.unsplash.com/photo-1539571696357-5a69c17a67c6?w=800&auto=format&fit=crop&q=80", IsPrimary = true, DisplayOrder = 1 }
                    },
                    Variants = new List<ProductVariant>
                    {
                        new() { VariantName = "38 / Camel Tan", SKU = "RYM-COAT-38", Price = 10999m, StockQuantity = 5 },
                        new() { VariantName = "40 / Camel Tan", SKU = "RYM-COAT-40", Price = 10999m, StockQuantity = 6 },
                        new() { VariantName = "42 / Camel Tan", SKU = "RYM-COAT-42", Price = 10999m, StockQuantity = 4 }
                    }
                }
            };

            foreach (var prod in newMensProducts)
            {
                if (!await context.Products.AnyAsync(p => p.Slug == prod.Slug))
                {
                    await context.Products.AddAsync(prod);
                }
            }

            await context.SaveChangesAsync();
        }

        private static async Task EnsureAllClothingHasSizesAsync(ApplicationDbContext context)
        {
            var clothingCategories = await context.Categories
                .Where(c => c.Slug == "mens-fashion" || c.Slug == "womens-fashion" || c.Slug == "sports-fitness")
                .Select(c => c.Id)
                .ToListAsync();

            if (!clothingCategories.Any()) return;

            var products = await context.Products
                .Include(p => p.Variants)
                .Where(p => clothingCategories.Contains(p.CategoryId))
                .ToListAsync();

            var variantsToAdd = new List<ProductVariant>();

            foreach (var prod in products)
            {
                if (prod.Variants != null && prod.Variants.Any())
                    continue; // Already has size variants

                var effectivePrice = prod.DiscountPrice ?? prod.BasePrice;
                var name = prod.Name.ToLowerInvariant();

                if (name.Contains("shoe") || name.Contains("sneaker") || name.Contains("oxford") || name.Contains("loafer") || name.Contains("boot"))
                {
                    // Footwear sizes
                    variantsToAdd.Add(new ProductVariant { ProductId = prod.Id, VariantName = "UK 7 (EU 41)", SKU = $"{prod.SKU}-UK7", Price = effectivePrice, StockQuantity = 10 });
                    variantsToAdd.Add(new ProductVariant { ProductId = prod.Id, VariantName = "UK 8 (EU 42)", SKU = $"{prod.SKU}-UK8", Price = effectivePrice, StockQuantity = 15 });
                    variantsToAdd.Add(new ProductVariant { ProductId = prod.Id, VariantName = "UK 9 (EU 43)", SKU = $"{prod.SKU}-UK9", Price = effectivePrice, StockQuantity = 15 });
                    variantsToAdd.Add(new ProductVariant { ProductId = prod.Id, VariantName = "UK 10 (EU 44)", SKU = $"{prod.SKU}-UK10", Price = effectivePrice, StockQuantity = 10 });
                }
                else if (name.Contains("jean") || name.Contains("trouser") || name.Contains("chino") || name.Contains("pant"))
                {
                    // Bottoms waist sizes: 30, 32, 34, 36, 38
                    variantsToAdd.Add(new ProductVariant { ProductId = prod.Id, VariantName = "Size 30 Waist", SKU = $"{prod.SKU}-30", Price = effectivePrice, StockQuantity = 12 });
                    variantsToAdd.Add(new ProductVariant { ProductId = prod.Id, VariantName = "Size 32 Waist", SKU = $"{prod.SKU}-32", Price = effectivePrice, StockQuantity = 18 });
                    variantsToAdd.Add(new ProductVariant { ProductId = prod.Id, VariantName = "Size 34 Waist", SKU = $"{prod.SKU}-34", Price = effectivePrice, StockQuantity = 15 });
                    variantsToAdd.Add(new ProductVariant { ProductId = prod.Id, VariantName = "Size 36 Waist", SKU = $"{prod.SKU}-36", Price = effectivePrice, StockQuantity = 10 });
                }
                else if (name.Contains("saree"))
                {
                    // Saree options
                    variantsToAdd.Add(new ProductVariant { ProductId = prod.Id, VariantName = "Standard (Unstitched Blouse)", SKU = $"{prod.SKU}-UNST", Price = effectivePrice, StockQuantity = 15 });
                    variantsToAdd.Add(new ProductVariant { ProductId = prod.Id, VariantName = "Stitched Blouse (Size 38 / M)", SKU = $"{prod.SKU}-ST38", Price = effectivePrice + 399m, StockQuantity = 10 });
                    variantsToAdd.Add(new ProductVariant { ProductId = prod.Id, VariantName = "Stitched Blouse (Size 40 / L)", SKU = $"{prod.SKU}-ST40", Price = effectivePrice + 399m, StockQuantity = 10 });
                }
                else if (name.Contains("suit") || name.Contains("blazer") || name.Contains("nehru") || name.Contains("overcoat") || name.Contains("kurta") || name.Contains("anarkali"))
                {
                    // Suit / Ethnic / Blazer chest sizes: 38, 40, 42, 44
                    variantsToAdd.Add(new ProductVariant { ProductId = prod.Id, VariantName = "Size 38 (Small)", SKU = $"{prod.SKU}-38", Price = effectivePrice, StockQuantity = 8 });
                    variantsToAdd.Add(new ProductVariant { ProductId = prod.Id, VariantName = "Size 40 (Medium)", SKU = $"{prod.SKU}-40", Price = effectivePrice, StockQuantity = 12 });
                    variantsToAdd.Add(new ProductVariant { ProductId = prod.Id, VariantName = "Size 42 (Large)", SKU = $"{prod.SKU}-42", Price = effectivePrice, StockQuantity = 10 });
                    variantsToAdd.Add(new ProductVariant { ProductId = prod.Id, VariantName = "Size 44 (XL)", SKU = $"{prod.SKU}-44", Price = effectivePrice, StockQuantity = 6 });
                }
                else if (name.Contains("watch"))
                {
                    // Watch strap / dial options
                    variantsToAdd.Add(new ProductVariant { ProductId = prod.Id, VariantName = "Standard 40mm Dial", SKU = $"{prod.SKU}-40MM", Price = effectivePrice, StockQuantity = 15 });
                    variantsToAdd.Add(new ProductVariant { ProductId = prod.Id, VariantName = "Large 44mm Dial", SKU = $"{prod.SKU}-44MM", Price = effectivePrice + 200m, StockQuantity = 15 });
                }
                else
                {
                    // Standard Apparel: S, M, L, XL, XXL (T-Shirts, Shirts, Hoodies, Tops, Dresses, Sweatshirts, Jackets, Joggers)
                    variantsToAdd.Add(new ProductVariant { ProductId = prod.Id, VariantName = "Size S (Small)", SKU = $"{prod.SKU}-S", Price = effectivePrice, StockQuantity = 12 });
                    variantsToAdd.Add(new ProductVariant { ProductId = prod.Id, VariantName = "Size M (Medium)", SKU = $"{prod.SKU}-M", Price = effectivePrice, StockQuantity = 20 });
                    variantsToAdd.Add(new ProductVariant { ProductId = prod.Id, VariantName = "Size L (Large)", SKU = $"{prod.SKU}-L", Price = effectivePrice, StockQuantity = 20 });
                    variantsToAdd.Add(new ProductVariant { ProductId = prod.Id, VariantName = "Size XL (Extra Large)", SKU = $"{prod.SKU}-XL", Price = effectivePrice, StockQuantity = 15 });
                    variantsToAdd.Add(new ProductVariant { ProductId = prod.Id, VariantName = "Size XXL (2XL)", SKU = $"{prod.SKU}-XXL", Price = effectivePrice, StockQuantity = 8 });
                }
            }

            if (variantsToAdd.Any())
            {
                await context.ProductVariants.AddRangeAsync(variantsToAdd);
                await context.SaveChangesAsync();
            }
        }
    }
}


