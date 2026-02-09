using System.ComponentModel.DataAnnotations;
using ondock.api.Data.Entities;

namespace ondock.api.DTOs.Post;

public class CreatePostDto
{
    [Required]
    public double Latitude { get; set; }

    [Required]
    public double Longitude { get; set; }

    [MaxLength(100)]
    public string? RoadIdentification { get; set; }

    public PostType PostType { get; set; } = PostType.Video;

    [MaxLength(500)]
    public string? MediaUrl { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    public UrgencyLevel UrgencyLevel { get; set; } = UrgencyLevel.Low;

    public int ExpirationHours { get; set; } = 24;

    public int VideoDurationSeconds { get; set; }

    public bool HasAudio { get; set; }
}
