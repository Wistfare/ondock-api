using System.ComponentModel.DataAnnotations;

namespace ondock.api.DTOs.Chat;

public class GenerateQRRequest
{
    [Required]
    public Guid ChatId { get; set; }

    [Required]
    public string TargetAnonymousUserId { get; set; } = string.Empty;

    public int ExpiryMinutes { get; set; } = 5;
}
