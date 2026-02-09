using System.ComponentModel.DataAnnotations;

namespace ondock.api.DTOs.Auth;

public class ChangePasswordRequest
{
    [Required(ErrorMessage = "Current password is required")]
    [MaxLength(100, ErrorMessage = "Password cannot exceed 100 characters")]
    public string CurrentPassword { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "New password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    [MaxLength(100, ErrorMessage = "Password cannot exceed 100 characters")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]+$", 
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character")]
    public string NewPassword { get; set; } = string.Empty;
}
