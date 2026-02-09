using System.ComponentModel.DataAnnotations;
using ondock.api.Attributes;

namespace ondock.api.DTOs.Auth;

public class LoginRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [MaxLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
    [SanitizeInput]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [MaxLength(100, ErrorMessage = "Password cannot exceed 100 characters")]
    public string Password { get; set; } = string.Empty;

    [MaxLength(100, ErrorMessage = "Device ID cannot exceed 100 characters")]
    [SanitizeInput]
    public string? DeviceId { get; set; }
}
