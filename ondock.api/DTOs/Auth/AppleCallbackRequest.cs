using System.ComponentModel.DataAnnotations;

namespace ondock.api.DTOs.Auth;

public class AppleCallbackRequest
{
    [Required]
    public string Code { get; set; } = string.Empty;
    
    [Required]
    public string IdToken { get; set; } = string.Empty;
    
    public string? State { get; set; }
    
    public string? User { get; set; } // Apple sends user info on first sign-in only
}
