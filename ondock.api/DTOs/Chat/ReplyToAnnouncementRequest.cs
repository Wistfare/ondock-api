using System.ComponentModel.DataAnnotations;

namespace ondock.api.DTOs.Chat;

public class ReplyToAnnouncementRequest
{
    [Required]
    public string AnnouncementId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string TargetAnonymousUserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Message { get; set; } = string.Empty;
}
