using ondock.api.Data.Entities;

namespace ondock.api.DTOs.Post;

public class PostDto
{
    public Guid PostId { get; set; }
    public Guid UserId { get; set; }
    public string? UserDisplayName { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? RoadIdentification { get; set; }
    public PostType PostType { get; set; }
    public string? MediaUrl { get; set; }
    public string? Description { get; set; }
    public UrgencyLevel UrgencyLevel { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int ViewCount { get; set; }
    public int VideoDurationSeconds { get; set; }
    public bool HasAudio { get; set; }
    public double? DistanceMeters { get; set; }
}
