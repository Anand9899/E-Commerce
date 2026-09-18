using System;
using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Application.DTOs
{
    public class ReviewVM
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? UserAvatar { get; set; }
        public int Rating { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public bool IsVerifiedPurchase { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateReviewDto
    {
        [Required]
        public int ProductId { get; set; }

        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5 stars")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Review comment is required")]
        [StringLength(1000)]
        public string Comment { get; set; } = string.Empty;
    }
}
