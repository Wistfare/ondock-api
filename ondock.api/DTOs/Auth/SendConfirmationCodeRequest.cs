using System.ComponentModel.DataAnnotations;

namespace ondock.api.DTOs.Auth;

public class SendConfirmationCodeRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
