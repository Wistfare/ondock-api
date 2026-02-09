using System.ComponentModel.DataAnnotations;

namespace ondock.api.DTOs.Auth;

public class RefreshTokenRequest
{
    [Required(ErrorMessage = "Refresh token is required")]
    [MaxLength(500, ErrorMessage = "Token is invalid")]
    public string RefreshToken { get; set; } = string.Empty;
}
