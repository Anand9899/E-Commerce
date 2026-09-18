using System.Collections.Generic;
using Ecommerce.Domain.Common;

namespace Ecommerce.Domain.Entities
{
    public class Category : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? IconClass { get; set; }
        public int DisplayOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public bool ShowOnHome { get; set; } = true;

        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
