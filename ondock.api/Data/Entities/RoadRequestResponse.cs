using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ondock.api.Data.Entities;

public class RoadRequestResponse
{
    [Key]
    public Guid ResponseId { get; set; }
    
    [Required]
    public Guid RequestId { get; set; }
    
    [Required]
    public Guid ResponderId { get; set; }
    
    [MaxLength(500)]
    public string? MediaUrl { get; set; }
    
    public PostType MediaType { get; set; }
    
    public int VideoDurationSeconds { get; set; }
    
    [MaxLength(500)]
    public string? Caption { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public bool? WasHelpful { get; set; }

    [MaxLength(500)]
    public string? ThumbnailUrl { get; set; }

    public bool IsLiveStream { get; set; }

    [MaxLength(255)]
    public string? LiveKitRoomName { get; set; }

    public int RewardPoints { get; set; }
    
    // Navigation properties
    [ForeignKey(nameof(RequestId))]
    public virtual RoadRequest Request { get; set; } = null!;
    
    [ForeignKey(nameof(ResponderId))]
    public virtual User Responder { get; set; } = null!;
}
