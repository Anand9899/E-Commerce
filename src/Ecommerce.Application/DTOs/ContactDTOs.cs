using System;
using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Application.DTOs
{
    public class ContactFormVM
    {
        [Required(ErrorMessage = "Please enter your name")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your email address")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Please enter a valid phone number")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Please enter a subject")]
        [StringLength(150, MinimumLength = 3, ErrorMessage = "Subject must be between 3 and 150 characters")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please write your message or complaint")]
        [StringLength(2000, MinimumLength = 10, ErrorMessage = "Message must be between 10 and 2000 characters")]
        public string Message { get; set; } = string.Empty;
    }

    public class ContactMessageListVM
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string MessageSnippet { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public bool IsReplied { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ContactMessageDetailVM
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public bool IsReplied { get; set; }
        public string? AdminNotes { get; set; }
        public string? RepliedBy { get; set; }
        public DateTime? RepliedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
