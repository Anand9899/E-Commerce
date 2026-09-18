using System;
using Ecommerce.Domain.Common;

namespace Ecommerce.Domain.Entities
{
    public class ContactMessage : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
        public bool IsReplied { get; set; } = false;
        public string? AdminNotes { get; set; }
        public string? RepliedBy { get; set; }
        public DateTime? RepliedAt { get; set; }
    }
}
