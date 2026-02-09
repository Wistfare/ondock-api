using System.ComponentModel.DataAnnotations;
using ondock.api.Attributes;

namespace ondock.api.DTOs.Auth;

public class ResendConfirmationEmailRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [MaxLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
    [SanitizeInput]
    public string Email { get; set; } = string.Empty;
}
