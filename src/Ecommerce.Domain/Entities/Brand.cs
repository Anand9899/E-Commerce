using System.Collections.Generic;
using Ecommerce.Domain.Common;

namespace Ecommerce.Domain.Entities
{
    public class Brand : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;

        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
