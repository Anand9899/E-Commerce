using System;
using Ecommerce.Domain.Common;
using Ecommerce.Domain.Enums;

namespace Ecommerce.Domain.Entities
{
    public class WalletTransaction : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser User { get; set; } = null!;

        public decimal Amount { get; set; }
        public WalletTransactionType Type { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal BalanceAfter { get; set; }
        public int? OrderId { get; set; }
        public virtual Order? Order { get; set; }
    }
}
