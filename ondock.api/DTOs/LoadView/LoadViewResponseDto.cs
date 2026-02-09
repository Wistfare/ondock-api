using ondock.api.Data.Entities;

namespace ondock.api.DTOs.LoadView;

public class LoadViewResponseDto
{
    public Guid ResponseId { get; set; }
    public Guid RequestId { get; set; }
    public Guid ResponderId { get; set; }
    public string? ResponderName { get; set; }
    public string? ResponderProfilePicture { get; set; }
    public string? MediaUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public PostType MediaType { get; set; }
    public int VideoDurationSeconds { get; set; }
    public string? Caption { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool? WasHelpful { get; set; }
    public bool IsLiveStream { get; set; }
    public string? LiveKitRoomName { get; set; }
    public int RewardPoints { get; set; }
}
