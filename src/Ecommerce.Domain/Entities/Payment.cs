using System;
using Ecommerce.Domain.Common;
using Ecommerce.Domain.Enums;

namespace Ecommerce.Domain.Entities
{
    public class Payment : BaseEntity
    {
        public int OrderId { get; set; }
        public virtual Order Order { get; set; } = null!;

        public string TransactionId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public PaymentMethod Method { get; set; }
        public PaymentStatus Status { get; set; }
        public DateTime? PaidAt { get; set; }
        public string? ProviderResponse { get; set; }
    }
}
