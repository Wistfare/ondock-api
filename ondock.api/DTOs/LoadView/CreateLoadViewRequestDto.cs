using System.ComponentModel.DataAnnotations;
using ondock.api.Data.Entities;

namespace ondock.api.DTOs.LoadView;

public class CreateLoadViewRequestDto
{
    [Required]
    public double Latitude { get; set; }

    [Required]
    public double Longitude { get; set; }

    public int RadiusMeters { get; set; } = 1609; // Default 1 mile

    [MaxLength(100)]
    public string? RequestType { get; set; }

    public UrgencyLevel UrgencyLevel { get; set; } = UrgencyLevel.Medium;

    public int ExpirationMinutes { get; set; } = 30;

    [MaxLength(500)]
    public string? Description { get; set; }
}
