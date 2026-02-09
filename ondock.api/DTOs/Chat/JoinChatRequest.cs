using System.ComponentModel.DataAnnotations;

namespace ondock.api.DTOs.Chat;

public class JoinChatRequest
{
    [Required]
    public double Latitude { get; set; }

    [Required]
    public double Longitude { get; set; }

    [Required]
    public double SpeedMph { get; set; }

    public double? RadiusMiles { get; set; }

    [Required]
    [MaxLength(100)]
    public string TargetAnonymousUserId { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? PublicKey { get; set; }
    
    /// <summary>
    /// Optional announcement ID. When provided, distance checks are skipped
    /// since announcement replies should work regardless of proximity.
    /// </summary>
    [MaxLength(100)]
    public string? AnnouncementId { get; set; }
}
