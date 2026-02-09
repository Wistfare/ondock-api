using System.ComponentModel.DataAnnotations;

namespace ondock.api.DTOs.Chat;

public class ReportChatRequest
{
    [MaxLength(100)]
    public string? TargetAnonymousUserId { get; set; }

    public string? ChatId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Reason { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Details { get; set; }
}
