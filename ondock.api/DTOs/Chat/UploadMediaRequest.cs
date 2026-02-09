using System.ComponentModel.DataAnnotations;

namespace ondock.api.DTOs.Chat;

public class UploadMediaRequest
{
    [Required]
    public string ChatId { get; set; } = string.Empty;

    [Required]
    public IFormFile File { get; set; } = null!;

    /// <summary>
    /// Optional caption/message for the media
    /// </summary>
    public string? Caption { get; set; }

    /// <summary>
    /// Duration in seconds for audio/video files
    /// </summary>
    public int? DurationSeconds { get; set; }

    // Location for rule enforcement
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? SpeedMph { get; set; }
}

public class UploadMediaResponse
{
    public string MessageId { get; set; } = string.Empty;
    public string MediaUrl { get; set; } = string.Empty;
    public string MediaType { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public long FileSize { get; set; }
    public int? DurationSeconds { get; set; }
    public DateTime Timestamp { get; set; }
}
