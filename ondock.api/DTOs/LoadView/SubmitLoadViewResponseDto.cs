using System.ComponentModel.DataAnnotations;
using ondock.api.Data.Entities;

namespace ondock.api.DTOs.LoadView;

public class SubmitLoadViewResponseDto
{
    [Required]
    [MaxLength(500)]
    public string MediaUrl { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ThumbnailUrl { get; set; }

    public PostType MediaType { get; set; } = PostType.Video;

    public int VideoDurationSeconds { get; set; }

    [MaxLength(500)]
    public string? Caption { get; set; }
}
