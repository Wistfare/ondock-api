using System.ComponentModel.DataAnnotations;

namespace ondock.api.DTOs.Auth;

public class VerifyConfirmationCodeRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(10, MinimumLength = 4)]
    public string Code { get; set; } = string.Empty;
}
