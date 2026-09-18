using System;
using Microsoft.AspNetCore.Identity;

namespace Ecommerce.Domain.Entities
{
    public class ApplicationRole : IdentityRole
    {
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ApplicationRole() : base() { }
        public ApplicationRole(string roleName) : base(roleName) { }
    }
}
