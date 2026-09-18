using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Application.Interfaces;
using Ecommerce.Infrastructure.Repositories;
using Ecommerce.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// =========================================================================
// 1. DATABASE CONFIGURATION (Entity Framework Core & SQL Server)
// =========================================================================
// appsettings.json se connection string read karke SQL Server DbContext configure karta hai
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, b => b.MigrationsAssembly("Ecommerce.Infrastructure")));

// =========================================================================
// 2. IDENTITY & AUTHENTICATION CONFIGURATION (Users, Roles & Passwords)
// =========================================================================
// User login, registration, password rules aur role management configure karta hai
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// =========================================================================
// 3. APPLICATION COOKIE CONFIGURATION (Session Persistence & Redirects)
// =========================================================================
// Login paths, access denied paths aur login session duration (14 days) set karta hai
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
});

// =========================================================================
// 4. SESSION & IN-MEMORY CACHE CONFIGURATION
// =========================================================================
// Cart items aur temporary session data ko memory me store karne ke liye
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// =========================================================================
// 5. DEPENDENCY INJECTION (Services & Repositories Registration)
// =========================================================================
// Business logic services aur data repositories ko Controllers me inject karne ke liye
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IBrandService, BrandService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<ICouponService, CouponService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IWishlistService, WishlistService>();
builder.Services.AddScoped<IAddressService, AddressService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddSingleton<IEmailService, EmailService>();
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<IWalletService, WalletService>();

// =========================================================================
// 6. PERFORMANCE & RESPONSE COMPRESSION (Brotli / Gzip)
// =========================================================================
// HTML, CSS, JS, JSON data ko compress karke transfer speed badhata hai
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllersWithViews();

var app = builder.Build();

// =========================================================================
// 7. DATABASE SEEDING (Auto-create Tables, Roles, Admin & Default Products)
// =========================================================================
// Pehli baar run hone par default roles, admin user aur sample products insert karta hai
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        await DbInitializer.SeedAsync(context, userManager, roleManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// =========================================================================
// 8. HTTP REQUEST PIPELINE & MIDDLEWARE
// =========================================================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Fast content transfer ke liye compression activate karta hai
app.UseResponseCompression();
app.UseHttpsRedirection();

// Static files (CSS, JS, Images, Fonts) ko 7 dino ke liye browser cache karta hai
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=604800");
    }
});

app.UseRouting();

// User session enable karta hai
app.UseSession();

// User Login status aur Role Authorization check karta hai
app.UseAuthentication();
app.UseAuthorization();

// =========================================================================
// 9. ROUTING CONFIGURATION (Admin Area & Customer Routes)
// =========================================================================
// Admin Panel routes: /Admin/Dashboard, /Admin/Products, /Admin/Messages, etc.
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

// Customer / Public routes: /Home/Index, /Product/Detail, etc.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
