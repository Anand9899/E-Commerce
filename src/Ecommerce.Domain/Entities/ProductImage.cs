using Ecommerce.Domain.Common;

namespace Ecommerce.Domain.Entities
{
    public class ProductImage : BaseEntity
    {
        public int ProductId { get; set; }
        public virtual Product Product { get; set; } = null!;
        public string ImageUrl { get; set; } = string.Empty;
        public string? AltText { get; set; }
        public bool IsPrimary { get; set; } = false;
        public int DisplayOrder { get; set; } = 0;
    }
}
