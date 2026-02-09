using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ondock.api.Data.Entities;

public enum MessageType
{
    Text = 0,
    Image = 1,
    Video = 2,
    Audio = 3,
    File = 4
}

[Table("Messages")]
public class Message
{
    [Key]
    public Guid MessageId { get; set; }

    [Required]
    [Column("RoomId")]
    [MaxLength(50)]
    public string ChatId { get; set; } = string.Empty;

    [Required]
    public Guid SenderId { get; set; }

    [Required]
    public string EncryptedContent { get; set; } = string.Empty;

    public MessageType MessageType { get; set; } = MessageType.Text;

    /// <summary>
    /// URL to the media file (for Image, Video, Audio, File types)
    /// </summary>
    public string? MediaUrl { get; set; }

    /// <summary>
    /// Original filename for media files
    /// </summary>
    public string? MediaFileName { get; set; }

    /// <summary>
    /// File size in bytes for media files
    /// </summary>
    public long? MediaFileSize { get; set; }

    /// <summary>
    /// Duration in seconds for audio/video files
    /// </summary>
    public int? MediaDurationSeconds { get; set; }

    /// <summary>
    /// ID of the announcement this message is replying to (if any)
    /// </summary>
    public Guid? AnnouncementId { get; set; }

    /// <summary>
    /// Quoted content when replying to an announcement
    /// </summary>
    public string? QuotedContent { get; set; }

    /// <summary>
    /// Title of the quoted content (e.g., "Announcement")
    /// </summary>
    public string? QuotedTitle { get; set; }

    public DateTime Timestamp { get; set; }

    /// <summary>
    /// When the message was read by the recipient (null if not yet read)
    /// </summary>
    public DateTime? ReadAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(ChatId))]
    public virtual Chat Chat { get; set; } = null!;

    [ForeignKey(nameof(SenderId))]
    public virtual ChatParticipant Sender { get; set; } = null!;
}
