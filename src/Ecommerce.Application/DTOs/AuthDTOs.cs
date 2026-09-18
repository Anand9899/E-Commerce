using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Application.DTOs
{
    public class LoginVM
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; } = false;
        public string? ReturnUrl { get; set; }
    }

    public class RegisterVM
    {
        [Required(ErrorMessage = "Full Name is required")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@gmail\.com$", ErrorMessage = "Only valid @gmail.com email addresses are allowed (e.g. yourname@gmail.com).")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mobile number is required")]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Mobile number must be exactly 10 digits without country code (e.g. 9876543210).")]
        [Display(Name = "Mobile Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select your gender")]
        public string? Gender { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long")]
        [RegularExpression(@"^(?=.*[0-9])(?=.*[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?])[A-Z].*$", ErrorMessage = "Password must start with a Capital letter (A-Z) and contain at least one number and one special symbol (e.g. Anand@123).")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }
    }

    public class UserProfileVM
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Gender { get; set; }
        public string? AvatarUrl { get; set; }
        public string? PlainPassword { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public int TotalOrders { get; set; }
        public int WishlistCount { get; set; }
        public int AddressCount { get; set; }
        public string Role { get; set; } = "Customer";
    }

    public class ForgotPasswordVM
    {
        [Required(ErrorMessage = "Registered email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@gmail\.com$", ErrorMessage = "Only valid @gmail.com email addresses are allowed (e.g. yourname@gmail.com).")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long")]
        [RegularExpression(@"^(?=.*[0-9])(?=.*[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?])[A-Z].*$", ErrorMessage = "Password must start with a Capital letter (A-Z) and contain at least one number and one special symbol (e.g. Anand@123).")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your new password")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}

