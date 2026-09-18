namespace Ecommerce.Application.DTOs
{
    public class CategoryVM
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? IconClass { get; set; }
        public int ProductCount { get; set; }
        public bool ShowOnHome { get; set; }
    }

    public class BrandVM
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public int ProductCount { get; set; }
    }
}
