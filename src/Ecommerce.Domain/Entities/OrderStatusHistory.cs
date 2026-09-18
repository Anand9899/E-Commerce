using Ecommerce.Domain.Common;
using Ecommerce.Domain.Enums;

namespace Ecommerce.Domain.Entities
{
    public class OrderStatusHistory : BaseEntity
    {
        public int OrderId { get; set; }
        public virtual Order Order { get; set; } = null!;

        public OrderStatus Status { get; set; }
        public string? Notes { get; set; }
        public string? ChangedBy { get; set; }
    }
}
