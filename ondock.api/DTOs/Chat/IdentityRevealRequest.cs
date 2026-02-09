using System.ComponentModel.DataAnnotations;

namespace ondock.api.DTOs.Chat;

public class IdentityRevealRequest
{
    [Required]
    public string ChatId { get; set; } = string.Empty;

    [Required]
    public Guid TargetUserId { get; set; }
}
