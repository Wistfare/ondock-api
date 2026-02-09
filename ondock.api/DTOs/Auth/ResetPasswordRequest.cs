using System.ComponentModel.DataAnnotations;
using ondock.api.Attributes;

namespace ondock.api.DTOs.Auth;

public class ResetPasswordRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [MaxLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
    [SanitizeInput]
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Token is required")]
    [MaxLength(500, ErrorMessage = "Token is invalid")]
    public string Token { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "New password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    [MaxLength(100, ErrorMessage = "Password cannot exceed 100 characters")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]+$", 
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character")]
    public string NewPassword { get; set; } = string.Empty;
}
