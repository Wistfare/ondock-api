using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using ondock.api.Data.Entities;

namespace ondock.api.DTOs.Chat;

public class SendMediaMessageRequest
{
    [Required]
    public string ChatId { get; set; } = string.Empty;

    [Required]
    public IFormFile File { get; set; } = null!;

    public MessageType MessageType { get; set; }

    [MaxLength(500)]
    public string? Caption { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? SpeedMph { get; set; }
}
