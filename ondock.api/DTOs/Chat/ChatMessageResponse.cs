using ondock.api.Data.Entities;

namespace ondock.api.DTOs.Chat;

public class ChatMessageResponse
{
    public Guid MessageId { get; set; }
    public string ChatId { get; set; } = string.Empty;

    public Guid SenderUserId { get; set; }
    public string SenderTemporaryUserId { get; set; } = string.Empty;
    public string SenderDisplayName { get; set; } = string.Empty;
    public string SenderDisplayColor { get; set; } = string.Empty;

    public string EncryptedContent { get; set; } = string.Empty;

    public MessageType MessageType { get; set; }

    // Media fields
    public string? MediaUrl { get; set; }
    public string? MediaFileName { get; set; }
    public long? MediaFileSize { get; set; }
    public int? MediaDurationSeconds { get; set; }

    // Reply to announcement
    public Guid? AnnouncementId { get; set; }
    public string? QuotedContent { get; set; }
    public string? QuotedTitle { get; set; }

    public DateTime Timestamp { get; set; }

    /// <summary>
    /// When the message was read by the recipient (null if not yet read)
    /// </summary>
    public DateTime? ReadAt { get; set; }
}
