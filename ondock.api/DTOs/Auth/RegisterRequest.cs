using System.ComponentModel.DataAnnotations;
using ondock.api.Attributes;

namespace ondock.api.DTOs.Auth;

public class RegisterRequest
{
    [MaxLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
    [NoSpecialCharacters(" -'")]
    [SanitizeInput]
    public string? FirstName { get; set; }
    
    [MaxLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
    [NoSpecialCharacters(" -'")]
    [SanitizeInput]
    public string? LastName { get; set; }

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [MaxLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
    [SanitizeInput]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    [MaxLength(100, ErrorMessage = "Password cannot exceed 100 characters")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]+$", 
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password confirmation is required")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Invalid phone number format")]
    [MaxLength(50, ErrorMessage = "Phone number cannot exceed 50 characters")]
    [SanitizeInput]
    public string? PhoneNumber { get; set; }

    [MaxLength(100, ErrorMessage = "Device ID cannot exceed 100 characters")]
    [SanitizeInput]
    public string? DeviceId { get; set; }
}
