using Ecommerce.Domain.Common;

namespace Ecommerce.Domain.Entities
{
    public class Address : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser User { get; set; } = null!;
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string AddressLine1 { get; set; } = string.Empty;
        public string? AddressLine2 { get; set; }
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string Country { get; set; } = "India";
        public bool IsDefault { get; set; } = false;
    }
}
