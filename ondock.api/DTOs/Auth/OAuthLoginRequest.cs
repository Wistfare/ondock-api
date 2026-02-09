using System.ComponentModel.DataAnnotations;
using ondock.api.Attributes;

namespace ondock.api.DTOs.Auth;

public class OAuthLoginRequest
{
    [Required(ErrorMessage = "ID token is required")]
    [MaxLength(10000, ErrorMessage = "Token is invalid")]
    public string IdToken { get; set; } = string.Empty;

    [Required(ErrorMessage = "Provider is required")]
    [MaxLength(50, ErrorMessage = "Provider name cannot exceed 50 characters")]
    [RegularExpression(@"^(google|apple|Google|Apple)$", ErrorMessage = "Provider must be either 'google' or 'apple'")]
    [SanitizeInput]
    public string Provider { get; set; } = string.Empty; // "Google" or "Apple"

    [MaxLength(100, ErrorMessage = "Device ID cannot exceed 100 characters")]
    [SanitizeInput]
    public string? DeviceId { get; set; }

    /// <summary>
    /// Optional email from OAuth provider (used if token doesn't contain email claim)
    /// </summary>
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [MaxLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
    [SanitizeInput]
    public string? Email { get; set; }
}
