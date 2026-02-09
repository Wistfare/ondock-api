using System.ComponentModel.DataAnnotations;

namespace ondock.api.DTOs.Chat;

public class CreateChatRequest
{
    [Required]
    public string TargetAnonymousUserId { get; set; } = string.Empty;

    [Required]
    public double Latitude { get; set; }

    [Required]
    public double Longitude { get; set; }

    public double SpeedMph { get; set; }

    public double? RadiusMiles { get; set; }

    [MaxLength(2000)]
    public string? PublicKey { get; set; }

    public Guid? AnnouncementId { get; set; }
}
