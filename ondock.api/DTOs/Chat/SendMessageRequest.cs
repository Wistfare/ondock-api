using System.ComponentModel.DataAnnotations;
using ondock.api.Data.Entities;

namespace ondock.api.DTOs.Chat;

public class SendMessageRequest
{
    [Required]
    public string ChatId { get; set; } = string.Empty;

    [Required]
    public string EncryptedContent { get; set; } = string.Empty;

    public MessageType MessageType { get; set; } = MessageType.Text;

    // Location for rule enforcement
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? SpeedMph { get; set; }

    // Reply to announcement
    public Guid? AnnouncementId { get; set; }
    public string? QuotedContent { get; set; }
    public string? QuotedTitle { get; set; }
}
